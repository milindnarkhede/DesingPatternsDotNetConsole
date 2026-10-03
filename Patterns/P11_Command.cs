namespace ShopKart.Patterns.Command;

// ===== RECEIVER: the object that actually changes =====
public class ProductStore
{
    private readonly Dictionary<int, Product> _products = new()
    {
        [1] = Catalog.Phone, [2] = Catalog.Headphones, [3] = Catalog.Charger
    };
    public decimal GetPrice(int id) => _products[id].Price;
    public void SetPrice(int id, decimal price) => _products[id] = _products[id] with { Price = price };
    public IEnumerable<int> Ids => _products.Keys.ToList();
    public string Show() => string.Join(", ", _products.Values.Select(p => $"{p.Name} {Rs(p.Price)}"));
}

// ===== COMMAND interface: an action as an object =====
public interface ICommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

// ===== CONCRETE COMMANDS: they carry the data + remember how to undo =====
public class ChangePriceCommand(ProductStore store, int productId, decimal newPrice) : ICommand
{
    private decimal _oldPrice;
    public string Description => $"Change price of product {productId} to {Rs(newPrice)}";

    public void Execute()
    {
        _oldPrice = store.GetPrice(productId);       // remember for Undo
        store.SetPrice(productId, newPrice);
    }
    public void Undo() => store.SetPrice(productId, _oldPrice);
}

public class FlashSaleCommand(ProductStore store, int percentOff) : ICommand
{
    private readonly Dictionary<int, decimal> _oldPrices = new();
    public string Description => $"Flash sale {percentOff}% off everything";

    public void Execute()
    {
        foreach (int id in store.Ids)
        {
            _oldPrices[id] = store.GetPrice(id);
            store.SetPrice(id, Math.Round(store.GetPrice(id) * (100 - percentOff) / 100m));
        }
    }
    public void Undo()
    {
        foreach (var (id, price) in _oldPrices) store.SetPrice(id, price);
    }
}

// ===== INVOKER: runs commands, keeps history, audit log and a queue =====
public class AdminCommandInvoker
{
    private readonly Stack<ICommand> _history = new();
    private readonly Queue<ICommand> _scheduled = new();
    public List<string> AuditLog { get; } = [];

    public void Run(ICommand command)
    {
        command.Execute();
        _history.Push(command);
        AuditLog.Add("RUN  " + command.Description);
        Out.Ok("Done:   " + command.Description);
    }

    public void Undo()
    {
        if (!_history.TryPop(out var command)) { Out.Fail("Nothing to undo"); return; }
        command.Undo();
        AuditLog.Add("UNDO " + command.Description);
        Out.Ok("Undone: " + command.Description);
    }

    public void Schedule(ICommand command)
    {
        _scheduled.Enqueue(command);
        Out.Step("Queued for midnight: " + command.Description);
    }

    public void RunScheduled()
    {
        while (_scheduled.TryDequeue(out var command)) Run(command);
    }
}

public static class CommandDemo
{
    public static void Run()
    {
        Out.Title("11 COMMAND — admin actions with undo, queue and audit log");

        var store = new ProductStore();
        var admin = new AdminCommandInvoker();
        Out.Info("Start:  " + store.Show());

        Out.Section("Admin changes the phone price, then undoes it");
        admin.Run(new ChangePriceCommand(store, 1, 13_999m));
        Out.Info("Now:    " + store.Show());
        admin.Undo();
        Out.Info("Now:    " + store.Show());

        Out.Section("Schedule a flash sale for midnight (command stored as an object)");
        admin.Schedule(new FlashSaleCommand(store, 20));
        Out.Info("Prices unchanged until midnight: " + store.Show());
        Out.Step("...midnight job runs...");
        admin.RunScheduled();
        Out.Info("Sale:   " + store.Show());
        admin.Undo();
        Out.Info("After:  " + store.Show());

        Out.Section("Audit log");
        foreach (string line in admin.AuditLog) Out.Info(line);
    }
}
