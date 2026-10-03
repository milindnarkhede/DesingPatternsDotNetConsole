using ShopKart.Patterns.Adapter;
using ShopKart.Patterns.Builder;
using ShopKart.Patterns.Strategy;

namespace ShopKart.Patterns.Facade;

// ===== SUBSYSTEMS: each one does one job =====
public class InventoryService
{
    private readonly Dictionary<int, int> _stock = new() { [1] = 5, [2] = 0, [3] = 20, [4] = 50 };

    public bool TryReserve(IEnumerable<CartItem> items, out string error)
    {
        foreach (var i in items)
            if (_stock.GetValueOrDefault(i.Product.Id) < i.Quantity)
            {
                error = $"{i.Product.Name} is out of stock";
                return false;
            }
        foreach (var i in items) _stock[i.Product.Id] -= i.Quantity;
        error = "";
        return true;
    }
    public void Release(IEnumerable<CartItem> items)
    {
        foreach (var i in items) _stock[i.Product.Id] += i.Quantity;
    }
    public int StockOf(int productId) => _stock.GetValueOrDefault(productId);
}

public class InvoiceService
{
    private int _next = 1;
    public string Create(Order o) => $"INV-{_next++:000}";
}

public class EmailService
{
    public void Send(string to, string message) => Out.Info($"[Email] to {to}: {message}");
}

public record PlaceOrderResult(bool Success, string Message);

// ===== FACADE: one simple method hides the whole workflow =====
public class OrderFacade(
    InventoryService inventory, IShippingProvider courier, InvoiceService invoices, EmailService email)
{
    private int _nextOrder = 1001;

    public PlaceOrderResult PlaceOrder(Order order, IPaymentStrategy payment)
    {
        string orderId = $"SK-{_nextOrder++}";

        Out.Step("1. Reserve stock");
        if (!inventory.TryReserve(order.Items, out string stockError))
            return new(false, stockError);

        Out.Step("2. Take payment");
        PaymentResult paid = payment.Pay(order.Total);
        if (!paid.Success)
        {
            inventory.Release(order.Items);                    // undo step 1
            return new(false, paid.Message + " → stock released");
        }

        Out.Step("3. Book courier");
        decimal weight = order.Items.Sum(i => i.Quantity) * 0.5m;
        TrackingInfo tracking = courier.Ship(new Shipment(orderId, weight, order.ShipTo.Pincode));

        Out.Step("4. Create invoice");
        string invoice = invoices.Create(order);

        Out.Step("5. Email customer");
        email.Send(order.Customer, $"Order {orderId} confirmed. Invoice {invoice}. Track {tracking.TrackingNo}");

        return new(true, $"{orderId} placed: {Rs(order.Total)} paid, arrives in {tracking.EtaDays} days");
    }
}

public static class FacadeDemo
{
    public static void Run()
    {
        Out.Title("07 FACADE — the website calls ONE method: PlaceOrder()");

        var inventory = new InventoryService();
        var facade = new OrderFacade(inventory, new BlueParcelAdapter(new BlueParcelApi()),
                                     new InvoiceService(), new EmailService());

        Order NewOrder(Product p) => new OrderBuilder().ForCustomer("Milind")
            .AddItem(p).ShipTo("Milind N", "Pune", "411001").Build();

        Out.Section("Order 1: everything OK");
        Print(facade.PlaceOrder(NewOrder(Catalog.Phone), new UpiPayment("demo@okhdfc")));

        Out.Section("Order 2: item out of stock");
        Print(facade.PlaceOrder(NewOrder(Catalog.Headphones), new UpiPayment("demo@okhdfc")));

        Out.Section("Order 3: payment fails, stock is given back");
        Out.Info($"Phone stock before: {inventory.StockOf(1)}");
        Print(facade.PlaceOrder(NewOrder(Catalog.Phone), new UpiPayment("wrong-id")));
        Out.Info($"Phone stock after:  {inventory.StockOf(1)}");
    }

    private static void Print(PlaceOrderResult r)
    {
        if (r.Success) Out.Ok(r.Message); else Out.Fail(r.Message);
    }
}
