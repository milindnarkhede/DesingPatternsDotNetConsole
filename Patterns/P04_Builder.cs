namespace ShopKart.Patterns.Builder;

public record Address(string Name, string City, string Pincode);

// ===== The finished product: immutable once built =====
public class Order
{
    public required string Customer { get; init; }
    public required IReadOnlyList<CartItem> Items { get; init; }
    public required Address ShipTo { get; init; }
    public string? Coupon { get; init; }
    public bool GiftWrap { get; init; }
    public decimal Discount { get; init; }
    public decimal Shipping { get; init; }

    public decimal Subtotal    => Items.Sum(i => i.LineTotal);
    public decimal GiftWrapFee => GiftWrap ? 50m : 0m;
    public decimal Total       => Subtotal - Discount + Shipping + GiftWrapFee;
}

// ===== The BUILDER: readable steps, validation in Build() =====
public class OrderBuilder
{
    private string? _customer;
    private readonly List<CartItem> _items = [];
    private Address? _shipTo;
    private string? _coupon;
    private bool _giftWrap;

    public OrderBuilder ForCustomer(string name)                     { _customer = name; return this; }
    public OrderBuilder AddItem(Product product, int quantity = 1)   { _items.Add(new(product, quantity)); return this; }
    public OrderBuilder ShipTo(string name, string city, string pin) { _shipTo = new(name, city, pin); return this; }
    public OrderBuilder WithCoupon(string code)                      { _coupon = code; return this; }
    public OrderBuilder WithGiftWrap()                               { _giftWrap = true; return this; }

    public Order Build()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(_customer)) errors.Add("customer is required");
        if (_items.Count == 0)                    errors.Add("add at least one item");
        if (_shipTo is null)                      errors.Add("shipping address is required");
        else if (_shipTo.Pincode.Length != 6 || !_shipTo.Pincode.All(char.IsDigit))
                                                  errors.Add($"pincode '{_shipTo.Pincode}' must be 6 digits");
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", errors));

        decimal subtotal = _items.Sum(i => i.LineTotal);
        decimal discount = _coupon == "SAVE10" ? subtotal * 0.10m : 0m;   // 10% off
        decimal shipping = subtotal >= 499m ? 0m : 49m;                    // free above ₹499

        return new Order
        {
            Customer = _customer!, Items = _items.ToList(), ShipTo = _shipTo!,
            Coupon = _coupon, GiftWrap = _giftWrap, Discount = discount, Shipping = shipping
        };
    }
}

public static class BuilderDemo
{
    public static void Run()
    {
        Out.Title("04 BUILDER — build an order step by step");

        Out.Section("Full order with coupon + gift wrap");
        Order big = new OrderBuilder()
            .ForCustomer("Milind")
            .AddItem(Catalog.Phone)
            .AddItem(Catalog.Charger, 2)
            .ShipTo("Milind N", "Pune", "411001")
            .WithCoupon("SAVE10")
            .WithGiftWrap()
            .Build();
        Print(big);

        Out.Section("Small order (shipping fee applies)");
        Print(new OrderBuilder().ForCustomer("Asha").AddItem(Catalog.Cover)
            .ShipTo("Asha K", "Mumbai", "400001").Build());

        Out.Section("Invalid order: Build() refuses it");
        try
        {
            new OrderBuilder().ForCustomer("Ravi").ShipTo("Ravi", "Delhi", "1100").Build();
        }
        catch (InvalidOperationException ex) { Out.Fail("Cannot build: " + ex.Message); }
    }

    private static void Print(Order o)
    {
        Out.Ok($"Order for {o.Customer} → {o.ShipTo.City} ({o.Items.Count} item line{(o.Items.Count == 1 ? "" : "s")})");
        Out.Info($"Subtotal {Rs(o.Subtotal)} - Discount {Rs(o.Discount)} + Shipping {Rs(o.Shipping)} + Gift wrap {Rs(o.GiftWrapFee)} = TOTAL {Rs(o.Total)}");
    }
}
