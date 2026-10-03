# ShopKart: 12 .NET Design Patterns in One E-commerce App

A small console app. Every design pattern is shown on a real online-shop feature:
payments, orders, couriers, the product catalogue, notifications and admin tools.
You don't need a database or any NuGet packages.

## Run it

You need the **.NET 8 SDK or newer**. Check with `dotnet --version`.

```bash
cd ShopKart.Patterns
dotnet run            # shows a menu; type 1-12 or all
dotnet run -- 1       # run only the Strategy demo
dotnet run -- all     # run all 12 demos
```

**Visual Studio 2022:** open `ShopKart.Patterns.csproj` and press F5.
**VS Code:** open the folder, then run `dotnet run` in the terminal.

## Where each pattern lives

| # | Pattern | File | ShopKart feature |
|---|---------|------|------------------|
| 01 | Strategy | Patterns/P01_Strategy.cs | Card / UPI / Cash on Delivery payment |
| 02 | Factory | Patterns/P02_Factory.cs | Create the payment object from the checkout form; keyed DI notifiers |
| 03 | Singleton | Patterns/P03_Singleton.cs | One StoreSettings object; DI lifetimes demo |
| 04 | Builder | Patterns/P04_Builder.cs | Build an Order with a coupon, gift wrap and address |
| 05 | Adapter | Patterns/P05_Adapter.cs | Plug FastShip (XML, grams) and BlueParcel (pounds) couriers |
| 06 | Decorator | Patterns/P06_Decorator.cs | Add cache + logging around the product database |
| 07 | Facade | Patterns/P07_Facade.cs | One PlaceOrder() call for stock, payment, courier, invoice and email |
| 08 | Observer | Patterns/P08_Observer.cs | The OrderPlaced event drives email, loyalty points and analytics |
| 09 | State | Patterns/P09_State.cs | Order lifecycle: New → Paid → Shipped → Delivered / Cancelled |
| 10 | Chain of Responsibility | Patterns/P10_ChainOfResponsibility.cs | Checkout checks: cart, stock, coupon, fraud |
| 11 | Command | Patterns/P11_Command.cs | Admin price changes with undo, a scheduled flash sale and an audit log |
| 12 | Repository + Unit of Work | Patterns/P12_RepositoryUnitOfWork.cs | Save the order and the stock change together (all or nothing) |

## Try these changes (good practice, and good for teaching)

1. **Strategy:** add `WalletPayment : IPaymentStrategy` and use it. You don't need to change `CheckoutService`.
2. **Factory:** add `"WALLET"` to `PaymentFactory`.
3. **Decorator:** write a `RetryProductService` and wrap it around the others.
4. **Observer:** add a `WarehouseHandler` that prints "pack the order".
5. **State:** add a `ReturnedState` that is allowed only after Delivered.
6. **Chain:** move `FraudHandler` to the start of the chain and watch the output change.
7. **Command:** add a `RedoStack` to the invoker.
8. **Unit of Work:** change Order B to ask for 2 headphones and see it commit.
