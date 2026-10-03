namespace ShopKart.Patterns.Observer;

// ===== The event data =====
public record OrderPlacedEvent(string OrderId, string Customer, decimal Total);

// ===== SUBJECT / PUBLISHER: knows nothing about its subscribers =====
public class OrderService
{
    public event EventHandler<OrderPlacedEvent>? OrderPlaced;

    public void PlaceOrder(string orderId, string customer, decimal total)
    {
        Out.Step($"Order {orderId} saved for {customer} ({Rs(total)}). Raising OrderPlaced...");
        var e = new OrderPlacedEvent(orderId, customer, total);

        // Call each subscriber safely: one failure must not stop the others
        foreach (EventHandler<OrderPlacedEvent> handler in OrderPlaced?.GetInvocationList() ?? [])
        {
            try { handler(this, e); }
            catch (Exception ex) { Out.Fail($"A subscriber failed: {ex.Message} (others still run)"); }
        }
    }
}

// ===== OBSERVERS / SUBSCRIBERS: each reacts in its own way =====
public class EmailReceiptHandler
{
    public void OnOrderPlaced(object? sender, OrderPlacedEvent e) =>
        Out.Ok($"[Email]     Receipt for {e.OrderId} sent to {e.Customer}");
}

public class LoyaltyPointsHandler
{
    private readonly Dictionary<string, int> _points = new();
    public void OnOrderPlaced(object? sender, OrderPlacedEvent e)
    {
        int earned = (int)(e.Total / 100);                    // 1 point per ₹100
        _points[e.Customer] = _points.GetValueOrDefault(e.Customer) + earned;
        Out.Ok($"[Loyalty]   +{earned} points, {e.Customer} now has {_points[e.Customer]}");
    }
}

public class AnalyticsHandler
{
    private int _orders; private decimal _revenue;
    public void OnOrderPlaced(object? sender, OrderPlacedEvent e)
    {
        _orders++; _revenue += e.Total;
        Out.Ok($"[Analytics] Today: orders = {_orders}, revenue = {Rs(_revenue)}");
    }
}

public class SmsHandler
{
    public void OnOrderPlaced(object? sender, OrderPlacedEvent e) =>
        throw new InvalidOperationException("SMS gateway is down");
}

public static class ObserverDemo
{
    public static void Run()
    {
        Out.Title("08 OBSERVER — one OrderPlaced event, many reactions");

        var orders = new OrderService();
        var email = new EmailReceiptHandler();
        var loyalty = new LoyaltyPointsHandler();
        var analytics = new AnalyticsHandler();

        orders.OrderPlaced += email.OnOrderPlaced;            // subscribe
        orders.OrderPlaced += loyalty.OnOrderPlaced;
        orders.OrderPlaced += analytics.OnOrderPlaced;

        Out.Section("Order 1: three subscribers");
        orders.PlaceOrder("SK-1001", "Milind", 17_000m);

        Out.Section("Add SMS subscriber (OrderService not changed) — but SMS is down");
        orders.OrderPlaced += new SmsHandler().OnOrderPlaced;
        orders.PlaceOrder("SK-1002", "Milind", 800m);

        Out.Section("Customer turns off emails: unsubscribe");
        orders.OrderPlaced -= email.OnOrderPlaced;
        orders.PlaceOrder("SK-1003", "Milind", 2_000m);
    }
}
