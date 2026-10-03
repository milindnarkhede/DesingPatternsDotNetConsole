namespace ShopKart.Patterns.Decorator;

// ===== COMPONENT interface =====
public interface IProductService
{
    Product? GetById(int id);
}

// ===== The real object: slow database lookup =====
public class DbProductService : IProductService
{
    private readonly Dictionary<int, Product> _table = new()
    {
        [1] = Catalog.Phone, [2] = Catalog.Headphones, [3] = Catalog.Charger
    };
    public int DbCalls { get; private set; }

    public Product? GetById(int id)
    {
        DbCalls++;
        Out.Info($"[DB]    SELECT * FROM Products WHERE Id = {id}");
        return _table.GetValueOrDefault(id);
    }
}

// ===== DECORATOR 1: adds caching, same interface =====
public class CachedProductService(IProductService inner) : IProductService
{
    private readonly Dictionary<int, Product?> _cache = new();

    public Product? GetById(int id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            Out.Info($"[Cache] hit for product {id}");
            return cached;
        }
        Out.Info($"[Cache] miss for product {id}");
        var product = inner.GetById(id);          // delegate to the wrapped object
        _cache[id] = product;
        return product;
    }
}

// ===== DECORATOR 2: adds logging, same interface =====
public class LoggingProductService(IProductService inner) : IProductService
{
    public Product? GetById(int id)
    {
        Out.Info($"[Log]   GetById({id}) called");
        var product = inner.GetById(id);
        Out.Info($"[Log]   GetById({id}) returned {product?.Name ?? "null"}");
        return product;
    }
}

public static class DecoratorDemo
{
    public static void Run()
    {
        Out.Title("06 DECORATOR — add cache + logging without editing DbProductService");

        var db = new DbProductService();

        // Wrap like layers of an onion: Logging( Cache( Database ) )
        IProductService products = new LoggingProductService(new CachedProductService(db));

        int[] pageViews = [1, 2, 1, 1];          // customers open product pages
        foreach (int id in pageViews)
        {
            Out.Step($"Customer opens product page {id}");
            var p = products.GetById(id);
            Out.Ok($"{p!.Name} {Rs(p.Price)}");
        }

        Out.Section("Result");
        Out.Ok($"{pageViews.Length} page views, only {db.DbCalls} database calls");
        Out.Info("In ASP.NET Core: services.AddScoped<IProductService>(sp => new LoggingProductService(new CachedProductService(new DbProductService())));");
    }
}
