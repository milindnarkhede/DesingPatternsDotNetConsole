using System.Text;
using ShopKart.Patterns.Builder;
using ShopKart.Patterns.Adapter;
using ShopKart.Patterns.Chain;
using ShopKart.Patterns.Command;
using ShopKart.Patterns.Decorator;
using ShopKart.Patterns.Facade;
using ShopKart.Patterns.Factory;
using ShopKart.Patterns.Observer;
using ShopKart.Patterns.Repository;
using ShopKart.Patterns.Singleton;
using ShopKart.Patterns.State;
using ShopKart.Patterns.Strategy;

Console.OutputEncoding = Encoding.UTF8;   

var demos = new (string Key, string Name, Action Run)[]
{
    ("1",  "Strategy",                StrategyDemo.Run),
    ("2",  "Factory",                 FactoryDemo.Run),
    ("3",  "Singleton",               SingletonDemo.Run),
    ("4",  "Builder",                 BuilderDemo.Run),
    ("5",  "Adapter",                 AdapterDemo.Run),
    ("6",  "Decorator",               DecoratorDemo.Run),
    ("7",  "Facade",                  FacadeDemo.Run),
    ("8",  "Observer",                ObserverDemo.Run),
    ("9",  "State",                   StateDemo.Run),
    ("10", "Chain of Responsibility", ChainDemo.Run),
    ("11", "Command",                 CommandDemo.Run),
    ("12", "Repository + Unit of Work", RepositoryDemo.Run),
};

// Usage:  dotnet run            → menu
//         dotnet run -- 7       → run pattern 7
//         dotnet run -- all     → run everything
string? choice = args.FirstOrDefault();

if (choice is null)
{
    Console.WriteLine("ShopKart — .NET design patterns demo");
    foreach (var d in demos) Console.WriteLine($"  {d.Key,2}. {d.Name}");
    Console.Write("Choose 1-12 or 'all': ");
    choice = Console.ReadLine()?.Trim();
}

if (string.Equals(choice, "all", StringComparison.OrdinalIgnoreCase))
{
    foreach (var d in demos) d.Run();
}
else
{
    var demo = demos.FirstOrDefault(d => d.Key == choice);
    if (demo.Run is null) Console.WriteLine($"Unknown choice '{choice}'. Use 1-12 or all.");
    else demo.Run();
}
Console.WriteLine();
