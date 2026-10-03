namespace ShopKart.Patterns.State;

// ===== STATE interface: every action an order can receive =====
public interface IOrderState
{
    string Name { get; }
    void Pay(OrderContext order);
    void Ship(OrderContext order);
    void Deliver(OrderContext order);
    void Cancel(OrderContext order);
}

// Base class: by default every action is NOT allowed
public abstract class OrderStateBase : IOrderState
{
    public abstract string Name { get; }
    public virtual void Pay(OrderContext o)     => Deny(o, "pay");
    public virtual void Ship(OrderContext o)    => Deny(o, "ship");
    public virtual void Deliver(OrderContext o) => Deny(o, "deliver");
    public virtual void Cancel(OrderContext o)  => Deny(o, "cancel");

    private void Deny(OrderContext o, string action) =>
        throw new InvalidOperationException($"Cannot {action} order {o.Id} because it is {Name}");
}

// ===== CONCRETE STATES: each one allows only what makes sense =====
public class NewState : OrderStateBase
{
    public override string Name => "New";
    public override void Pay(OrderContext o)    => o.MoveTo(new PaidState());
    public override void Cancel(OrderContext o) => o.MoveTo(new CancelledState());
}

public class PaidState : OrderStateBase
{
    public override string Name => "Paid";
    public override void Ship(OrderContext o) => o.MoveTo(new ShippedState());
    public override void Cancel(OrderContext o)
    {
        Out.Info($"Refund started for {o.Id}");
        o.MoveTo(new CancelledState());
    }
}

public class ShippedState : OrderStateBase
{
    public override string Name => "Shipped";
    public override void Deliver(OrderContext o) => o.MoveTo(new DeliveredState());
}

public class DeliveredState : OrderStateBase { public override string Name => "Delivered"; }
public class CancelledState : OrderStateBase { public override string Name => "Cancelled"; }

// ===== CONTEXT: the order. It asks its current state what to do =====
public class OrderContext(string id)
{
    public string Id { get; } = id;
    public IOrderState State { get; private set; } = new NewState();

    public void Pay()     => State.Pay(this);
    public void Ship()    => State.Ship(this);
    public void Deliver() => State.Deliver(this);
    public void Cancel()  => State.Cancel(this);

    internal void MoveTo(IOrderState next)
    {
        Out.Ok($"{Id}: {State.Name} → {next.Name}");
        State = next;
    }
}

public static class StateDemo
{
    public static void Run()
    {
        Out.Title("09 STATE — order lifecycle controls what is allowed");

        Out.Section("Happy path");
        var o1 = new OrderContext("SK-1001");
        Try(o1.Pay); Try(o1.Ship); Try(o1.Deliver);

        Out.Section("Cancel after payment → refund");
        var o2 = new OrderContext("SK-1002");
        Try(o2.Pay); Try(o2.Cancel);

        Out.Section("Invalid actions are blocked");
        var o3 = new OrderContext("SK-1003");
        Try(o3.Ship);                        // not paid yet
        Try(o3.Pay); Try(o3.Ship);
        Try(o3.Cancel);                      // already shipped
        Try(o1.Pay);                         // delivered order
    }

    private static void Try(Action action)
    {
        try { action(); }
        catch (InvalidOperationException ex) { Out.Fail(ex.Message); }
    }
}
