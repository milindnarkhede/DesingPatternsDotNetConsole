using System.Globalization;

namespace ShopKart.Common;

// ---------- Shared e-commerce models used by every demo ----------
public record Product(int Id, string Name, decimal Price);

public record CartItem(Product Product, int Quantity)
{
    public decimal LineTotal => Product.Price * Quantity;
}

public static class Catalog
{
    public static readonly Product Phone      = new(1, "Phone",      15_000m);
    public static readonly Product Headphones = new(2, "Headphones",  2_000m);
    public static readonly Product Charger    = new(3, "Charger",       800m);
    public static readonly Product Cover      = new(4, "Phone cover",   299m);
}

// ---------- Small helpers so the console output is easy to read ----------
public static class Money
{
    public static string Rs(decimal amount) =>
        "₹" + amount.ToString("#,0.##", CultureInfo.InvariantCulture);
}

public static class Out
{
    public static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 64));
        Console.WriteLine("  " + text);
        Console.WriteLine(new string('=', 64));
    }
    public static void Section(string text) { Console.WriteLine(); Console.WriteLine($"  -- {text} --"); }
    public static void Step(string text) => Console.WriteLine("  → " + text);
    public static void Ok(string text)   => Console.WriteLine("  ✔ " + text);
    public static void Fail(string text) => Console.WriteLine("  ✘ " + text);
    public static void Info(string text) => Console.WriteLine("      " + text);
}
