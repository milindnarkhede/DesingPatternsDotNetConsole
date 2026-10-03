using Microsoft.Extensions.DependencyInjection;
using ShopKart.Patterns.Strategy;

namespace ShopKart.Patterns.Factory;

// What the checkout form sends us, e.g. { "method": "UPI", "detail": "demo@okhdfc" }
public record PaymentChoice(string Method, string? Detail = null);

// ===== PART A: Simple Factory — ONE place knows every concrete class =====
public interface IPaymentFactory
{
    IPaymentStrategy Create(PaymentChoice choice);
}

public class PaymentFactory : IPaymentFactory
{
    public IPaymentStrategy Create(PaymentChoice choice) => choice.Method.ToUpperInvariant() switch
    {
        "CARD" => new CardPayment(choice.Detail ?? throw new ArgumentException("Card number is required")),
        "UPI"  => new UpiPayment(choice.Detail ?? throw new ArgumentException("UPI id is required")),
        "COD"  => new CashOnDelivery(),
        _      => throw new NotSupportedException($"Payment method '{choice.Method}' is not supported")
    };
}

// The caller only talks to the factory + interface. No 'new CardPayment' here.
public class CheckoutApi(IPaymentFactory factory)
{
    public void Post(PaymentChoice request, decimal total)
    {
        try
        {
            IPaymentStrategy payment = factory.Create(request);
            Out.Step($"Factory returned {payment.GetType().Name} for '{request.Method}'");
            var result = payment.Pay(total);
            if (result.Success) Out.Ok(result.Message); else Out.Fail(result.Message);
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
        {
            Out.Fail($"400 Bad Request: {ex.Message}");
        }
    }
}

// ===== PART B: .NET 8 keyed services — the DI container acts as the factory =====
public interface INotifier { string Send(string to, string message); }
public class EmailNotifier    : INotifier { public string Send(string to, string m) => $"Email to {to}: {m}"; }
public class SmsNotifier      : INotifier { public string Send(string to, string m) => $"SMS to {to}: {m}"; }
public class WhatsAppNotifier : INotifier { public string Send(string to, string m) => $"WhatsApp to {to}: {m}"; }

public static class FactoryDemo
{
    public static void Run()
    {
        Out.Title("02 FACTORY — create the right object from customer input");

        Out.Section("A) Simple factory: payment method chosen on checkout page");
        var api = new CheckoutApi(new PaymentFactory());
        api.Post(new PaymentChoice("UPI", "demo@okhdfc"), 2_000m);
        api.Post(new PaymentChoice("card", "4111222233339999"), 2_000m);
        api.Post(new PaymentChoice("COD"), 2_000m);
        api.Post(new PaymentChoice("Crypto"), 2_000m);          // unknown → clean error

        Out.Section("B) Keyed services (.NET 8): customer's preferred channel");
        var services = new ServiceCollection();
        services.AddKeyedTransient<INotifier, EmailNotifier>("email");
        services.AddKeyedTransient<INotifier, SmsNotifier>("sms");
        services.AddKeyedTransient<INotifier, WhatsAppNotifier>("whatsapp");
        using var provider = services.BuildServiceProvider();

        foreach (var channel in new[] { "sms", "whatsapp", "fax" })
        {
            INotifier? notifier = provider.GetKeyedService<INotifier>(channel);
            if (notifier is null) { Out.Fail($"No notifier registered for '{channel}'"); continue; }
            Out.Ok(notifier.Send("Milind", "Your order SK-1001 is confirmed"));
        }
    }
}
