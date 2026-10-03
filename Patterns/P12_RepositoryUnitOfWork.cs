namespace ShopKart.Patterns.Repository;

// ===== A tiny in-memory "database" (in a real app this is SQL Server via EF Core) =====
public record SavedOrder(string OrderId, string Customer, int ProductId, int Quantity, decimal Amount);

public class FakeDatabase
{
    public Dictionary<int, int> Stock { get; } = new() { [1] = 5, [2] = 2, [3] = 20 };
    public List<SavedOrder> Orders { get; } = [];
}

// ===== REPOSITORIES: collection-like access, no SQL in services =====
public interface IOrderRepository
{
    void Add(SavedOrder order);
    int Count();
}

public interface IStockRepository
{
    int Available(int productId);
    void Reduce(int productId, int quantity);
}

// ===== UNIT OF WORK: groups changes, saves all or nothing =====
public interface IUnitOfWork
{
    IOrderRepository Orders { get; }
    IStockRepository Stock { get; }
    void Commit();
    void Rollback();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly FakeDatabase _db;
    private readonly List<SavedOrder> _newOrders = [];          // pending inserts
    private readonly Dictionary<int, int> _stockReduced = [];    // pending updates

    public UnitOfWork(FakeDatabase db)
    {
        _db = db;
        Orders = new OrderRepository(db, _newOrders);
        Stock = new StockRepository(db, _stockReduced);
    }

    public IOrderRepository Orders { get; }
    public IStockRepository Stock { get; }

    public void Commit()
    {
        Out.Info("[DB] BEGIN TRANSACTION");
        foreach (var o in _newOrders)
        {
            _db.Orders.Add(o);
            Out.Info($"[DB]   INSERT Orders ({o.OrderId}, product {o.ProductId} x{o.Quantity})");
        }
        foreach (var (id, qty) in _stockReduced)
        {
            _db.Stock[id] -= qty;
            Out.Info($"[DB]   UPDATE Stock SET Qty = {_db.Stock[id]} WHERE ProductId = {id}");
        }
        Out.Info("[DB] COMMIT");
        Clear();
    }

    public void Rollback()
    {
        Out.Info($"[DB] ROLLBACK ({_newOrders.Count} pending inserts thrown away)");
        Clear();
    }

    private void Clear() { _newOrders.Clear(); _stockReduced.Clear(); }

    // --- repository implementations share the UoW's pending changes ---
    private class OrderRepository(FakeDatabase db, List<SavedOrder> pending) : IOrderRepository
    {
        public void Add(SavedOrder order) => pending.Add(order);
        public int Count() => db.Orders.Count;
    }

    private class StockRepository(FakeDatabase db, Dictionary<int, int> pending) : IStockRepository
    {
        public int Available(int id) => db.Stock.GetValueOrDefault(id) - pending.GetValueOrDefault(id);
        public void Reduce(int id, int qty)
        {
            if (Available(id) < qty)
                throw new InvalidOperationException($"not enough stock for product {id} (have {Available(id)}, need {qty})");
            pending[id] = pending.GetValueOrDefault(id) + qty;
        }
    }
}

// ===== SERVICE: business logic only, talks to UoW + repositories =====
public class OrderPlacementService(IUnitOfWork uow)
{
    public void PlaceOrder(string orderId, string customer, params (Product Product, int Qty)[] lines)
    {
        Out.Step($"Place {orderId} for {customer}");
        try
        {
            foreach (var (product, qty) in lines)
            {
                uow.Orders.Add(new SavedOrder(orderId, customer, product.Id, qty, product.Price * qty));
                uow.Stock.Reduce(product.Id, qty);
                Out.Info($"staged: {product.Name} x{qty}");
            }
            uow.Commit();                                   // ONE save for everything
            Out.Ok($"{orderId} saved");
        }
        catch (InvalidOperationException ex)
        {
            uow.Rollback();                                 // nothing half-saved
            Out.Fail($"{orderId} failed: {ex.Message}");
        }
    }
}

public static class RepositoryDemo
{
    public static void Run()
    {
        Out.Title("12 REPOSITORY + UNIT OF WORK — save order + stock together");

        var db = new FakeDatabase();
        var service = new OrderPlacementService(new UnitOfWork(db));
        void Show() => Out.Info($"DB now: {db.Orders.Count} order rows | stock Phone={db.Stock[1]}, Headphones={db.Stock[2]}, Charger={db.Stock[3]}");

        Show();
        Out.Section("Order A: Phone x1 + Charger x2 (all OK)");
        service.PlaceOrder("SK-2001", "Milind", (Catalog.Phone, 1), (Catalog.Charger, 2));
        Show();

        Out.Section("Order B: Phone x1 + Headphones x3 (only 2 headphones)");
        service.PlaceOrder("SK-2002", "Asha", (Catalog.Phone, 1), (Catalog.Headphones, 3));
        Show();
        Out.Info("Phone stock did NOT drop for the failed order: all or nothing.");
    }
}
