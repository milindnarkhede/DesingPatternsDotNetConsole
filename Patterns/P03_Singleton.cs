using Microsoft.Extensions.DependencyInjection;

namespace ShopKart.Patterns.Singleton;

// ===== PART A: Classic thread-safe Singleton with Lazy<T> =====
public sealed class StoreSettings
{
    private static readonly Lazy<StoreSettings> _instance = new(() => new StoreSettings());
    public static StoreSettings Instance => _instance.Value;     // the ONLY way to get it

    public static int TimesCreated { get; private set; }

    private StoreSettings()                                    // private: nobody can 'new' it
    {
        TimesCreated++;
        Out.Info("(loading store settings from config... this runs only once)");
    }

    public decimal FreeShippingAbove { get; } = 499m;
    public decimal GstPercent { get; } = 18m;
    public string Currency { get; } = "INR";
}

// Three different services all read the same object
public class CartService     { public string Info() => $"Cart: free shipping above {Rs(StoreSettings.Instance.FreeShippingAbove)}"; }
public class InvoiceService  { public string Info() => $"Invoice: GST {StoreSettings.Instance.GstPercent}%"; }
public class ReportService   { public string Info() => $"Report: currency {StoreSettings.Instance.Currency}"; }

// ===== PART B: The .NET way — let DI control lifetime =====
public abstract class Probe
{
    private static int _counter;
    public int InstanceNo { get; } = Interlocked.Increment(ref _counter);
}
public class SingletonProbe : Probe { }
public class ScopedProbe    : Probe { }
public class TransientProbe : Probe { }

public static class SingletonDemo
{
    public static void Run()
    {
        Out.Title("03 SINGLETON — one shared StoreSettings for the whole app");

        Out.Section("A) Classic singleton");
        Out.Ok(new CartService().Info());
        Out.Ok(new InvoiceService().Info());
        Out.Ok(new ReportService().Info());
        Out.Step($"Same object everywhere? {ReferenceEquals(StoreSettings.Instance, StoreSettings.Instance)}");

        Parallel.For(0, 1_000, i => { var s = StoreSettings.Instance; });   // 1,000 parallel calls
        Out.Step($"After 1,000 parallel calls, times created = {StoreSettings.TimesCreated} (thread-safe)");

        Out.Section("B) DI lifetimes: Singleton vs Scoped vs Transient");
        var services = new ServiceCollection();
        services.AddSingleton<SingletonProbe>();
        services.AddScoped<ScopedProbe>();
        services.AddTransient<TransientProbe>();
        using var provider = services.BuildServiceProvider();

        for (int request = 1; request <= 2; request++)
        {
            using var scope = provider.CreateScope();             // = one HTTP request
            var sp = scope.ServiceProvider;
            Out.Step($"HTTP request {request}: " +
                $"Singleton #{sp.GetRequiredService<SingletonProbe>().InstanceNo}, #{sp.GetRequiredService<SingletonProbe>().InstanceNo} | " +
                $"Scoped #{sp.GetRequiredService<ScopedProbe>().InstanceNo}, #{sp.GetRequiredService<ScopedProbe>().InstanceNo} | " +
                $"Transient #{sp.GetRequiredService<TransientProbe>().InstanceNo}, #{sp.GetRequiredService<TransientProbe>().InstanceNo}");
        }
        Out.Info("Singleton = same number always. Scoped = same within one request. Transient = new every time.");
    }
}
