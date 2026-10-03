namespace ShopKart.Patterns.Strategy;

// ===== Result returned by every payment method =====
public record PaymentResult(bool Success, string Message, decimal AmountCharged);

// ===== 1. STRATEGY INTERFACE: the common contract =====
public interface IPaymentStrategy
{
    string Name { get; }
    PaymentResult Pay(decimal amount);
}

// ===== 2. CONCRETE STRATEGIES: one class per payment method =====
public class CardPayment(string cardNumber) : IPaymentStrategy
{
    private const decimal FeePercent = 0.02m;               // 2% bank fee
    public string Name => "Card";

    public PaymentResult Pay(decimal amount)
    {
        decimal fee = amount * FeePercent;
        return new(true, $"Card ****{cardNumber[^4..]} charged {Rs(amount + fee)} (fee {Rs(fee)})", amount + fee);
    }
}

public class UpiPayment(string upiId) : IPaymentStrategy
{
    public string Name => "UPI";

    public PaymentResult Pay(decimal amount)
    {
        if (!upiId.Contains('@'))
            return new(false, $"Invalid UPI id '{upiId}'", 0);
        return new(true, $"UPI {upiId} paid {Rs(amount)} (no fee)", amount);
    }
}

public class CashOnDelivery : IPaymentStrategy
{
    private const decimal CodFee = 40m;
    private const decimal MaxCodAmount = 10_000m;
    public string Name => "Cash on Delivery";

    public PaymentResult Pay(decimal amount)
    {
        if (amount > MaxCodAmount)
            return new(false, $"COD not allowed above {Rs(MaxCodAmount)}", 0);
        return new(true, $"COD booked: collect {Rs(amount + CodFee)} at door (fee {Rs(CodFee)})", amount + CodFee);
    }
}

// ===== 3. CONTEXT: uses a strategy, never knows which one =====
public class CheckoutService
{
    private IPaymentStrategy? _payment;

    public void SetPayment(IPaymentStrategy payment) => _payment = payment;

    public PaymentResult Checkout(decimal cartTotal)
    {
        if (_payment is null)
            throw new InvalidOperationException("Choose a payment method first");

        Out.Step($"Checkout {Rs(cartTotal)} using {_payment.Name}");
        return _payment.Pay(cartTotal);                     // delegate the work
    }
}

// ===== DEMO =====
public static class StrategyDemo
{
    public static void Run()
    {
        Out.Title("01 STRATEGY — one checkout, many payment methods");

        var checkout = new CheckoutService();
        decimal cartTotal = Catalog.Phone.Price + Catalog.Headphones.Price;   // ₹17,000

        IPaymentStrategy[] customerChoices =
        [
            new CardPayment("4111222233334444"),
            new UpiPayment("demo@okhdfc"),
            new UpiPayment("demo-okhdfc"),      // typo: no '@'
            new CashOnDelivery()                  // too big for COD
        ];

        foreach (var choice in customerChoices)
        {
            checkout.SetPayment(choice);          // swap behaviour at runtime
            Print(checkout.Checkout(cartTotal));
        }

        Out.Section("Small cart can use COD");
        checkout.SetPayment(new CashOnDelivery());
        Print(checkout.Checkout(Catalog.Charger.Price));
    }

    private static void Print(PaymentResult r)
    {
        if (r.Success) Out.Ok(r.Message); else Out.Fail(r.Message);
    }
}
