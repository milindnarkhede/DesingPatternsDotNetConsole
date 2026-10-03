namespace ShopKart.Patterns.Chain;

public record CheckoutRequest(string Customer, List<CartItem> Items, string? Coupon, string PaymentMethod)
{
    public decimal Total => Items.Sum(i => i.LineTotal);
}

public record CheckoutResult(bool Approved, string Message);

// ===== HANDLER base: run my check, then pass to the next handler =====
public abstract class CheckoutHandler
{
    private CheckoutHandler? _next;

    public CheckoutHandler SetNext(CheckoutHandler next)
    {
        _next = next;
        return next;                                   // allows a.SetNext(b).SetNext(c)
    }

    public CheckoutResult Handle(CheckoutRequest request)
    {
        string? error = Check(request);
        if (error is not null)
        {
            Out.Fail($"{GetType().Name}: {error}");
            return new(false, $"Stopped by {GetType().Name}");   // STOP the chain
        }
        Out.Ok($"{GetType().Name} passed");
        return _next?.Handle(request) ?? new(true, "All checks passed. Go to payment");
    }

    protected abstract string? Check(CheckoutRequest request);   // null = OK
}

// ===== CONCRETE HANDLERS: one rule each =====
public class CartNotEmptyHandler : CheckoutHandler
{
    protected override string? Check(CheckoutRequest r) =>
        r.Items.Count == 0 ? "cart is empty" : null;
}

public class StockHandler : CheckoutHandler
{
    private readonly Dictionary<int, int> _stock = new() { [1] = 5, [2] = 1, [3] = 20, [4] = 50 };
    protected override string? Check(CheckoutRequest r)
    {
        var missing = r.Items.FirstOrDefault(i => _stock.GetValueOrDefault(i.Product.Id) < i.Quantity);
        return missing is null ? null : $"only {_stock.GetValueOrDefault(missing.Product.Id)} {missing.Product.Name} left";
    }
}

public class CouponHandler : CheckoutHandler
{
    protected override string? Check(CheckoutRequest r) => r.Coupon switch
    {
        null => null,
        "SAVE10" when r.Total >= 1_000m => null,
        "SAVE10" => "SAVE10 needs a cart of at least ₹1,000",
        _ => $"coupon '{r.Coupon}' does not exist"
    };
}

public class FraudHandler : CheckoutHandler
{
    protected override string? Check(CheckoutRequest r) =>
        r.PaymentMethod == "COD" && r.Total > 10_000m
            ? $"COD order of {Rs(r.Total)} needs manual review"
            : null;
}

public static class ChainDemo
{
    public static void Run()
    {
        Out.Title("10 CHAIN OF RESPONSIBILITY — checkout validation pipeline");

        // Build the chain: Cart → Stock → Coupon → Fraud
        var chain = new CartNotEmptyHandler();
        chain.SetNext(new StockHandler())
             .SetNext(new CouponHandler())
             .SetNext(new FraudHandler());

        Run(chain, "Good order", new("Milind", [new(Catalog.Phone, 1)], "SAVE10", "UPI"));
        Run(chain, "Empty cart", new("Asha", [], null, "UPI"));
        Run(chain, "Wants 2 headphones, only 1 left", new("Ravi", [new(Catalog.Headphones, 2)], null, "Card"));
        Run(chain, "SAVE10 on a small cart", new("Neha", [new(Catalog.Charger, 1)], "SAVE10", "UPI"));
        Run(chain, "Big COD order", new("Karan", [new(Catalog.Phone, 2)], null, "COD"));
    }

    private static void Run(CheckoutHandler chain, string title, CheckoutRequest request)
    {
        Out.Section(title);
        var result = chain.Handle(request);
        Out.Step("RESULT: " + result.Message);
    }
}
