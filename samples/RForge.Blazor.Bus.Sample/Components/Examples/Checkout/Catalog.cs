namespace RForge.Blazor.Bus.Sample.Components.Examples.Checkout;

public static class Catalog
{
    public static IReadOnlyList<Product> Products { get; } =
    [
        new(1, "Coffee beans", "☕", 14.50m),
        new(2, "Croissant", "🥐", 3.25m),
        new(3, "Avocado", "🥑", 1.90m),
        new(4, "Cheese board", "🧀", 18.00m),
        new(5, "Lemonade", "🍋", 4.75m),
        new(6, "Chocolate", "🍫", 6.20m),
    ];
}
