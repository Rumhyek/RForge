using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RForge.Blazor.Bus.Sample.Client.Shared;

// ── Shared counter (simple) ──────────────────────────────────────────────

public sealed record ClickState(int Count) : IState<ClickState>
{
    public static ClickState Initial => new(0);
}

// ── Notifications (simple) ───────────────────────────────────────────────

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Danger,
}

public sealed record ToastRequested(string Message, ToastLevel Level = ToastLevel.Info) : IEvent;

public sealed record NotificationStats(int Sent) : IState<NotificationStats>
{
    public static NotificationStats Initial => new(0);
}

// ── Plain components / theming (simple) ──────────────────────────────────

public sealed record ThemeState(string Name, string Background, string Foreground) : IState<ThemeState>
{
    public static ThemeState Initial => Light;

    public static ThemeState Light => new("Light", "#f8f9fa", "#212529");

    public static ThemeState Dark => new("Dark", "#212529", "#f8f9fa");

    public static ThemeState Ocean => new("Ocean", "#0b3d5c", "#d7f0ff");
}

// ── Scoped widgets (complex) ─────────────────────────────────────────────

public sealed record TodoItem(Guid Id, string Title, bool Done);

public sealed record TodoListState(string Title, ImmutableList<TodoItem> Items) : IState<TodoListState>
{
    public static TodoListState Initial => new("Untitled", []);
}

public sealed record TodoAdded(string List, string Title) : IEvent;

public sealed record TodoCompleted(string List, string Title, bool Done) : IEvent;

/// <summary>Published with Route.Local: never leaves the widget it was published in.</summary>
public sealed record ClearCompletedRequested : IEvent;

/// <summary>Published with Route.Global: reaches every widget.</summary>
public sealed record ResetAllRequested : IEvent;

// ── Checkout (complex) ───────────────────────────────────────────────────

public sealed record Product(int Id, string Name, string Emoji, decimal Price);

public sealed record CartLine(int ProductId, string Name, string Emoji, decimal Price, int Quantity);

public sealed record CartState(ImmutableList<CartLine> Lines, string? Source) : IState<CartState>
{
    public static CartState Initial => new([], null);

    [JsonIgnore]
    public int Count => Lines.Sum(static l => l.Quantity);

    [JsonIgnore]
    public decimal Total => Lines.Sum(static l => l.Price * l.Quantity);

    public CartState Add(Product product)
    {
        var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
        return this with
        {
            Lines = existing is null
                ? Lines.Add(new CartLine(product.Id, product.Name, product.Emoji, product.Price, 1))
                : Lines.Replace(existing, existing with { Quantity = existing.Quantity + 1 }),
        };
    }
}

public sealed record ItemAdded(Product Product) : IEvent;

public enum CheckoutStatus
{
    Editing,
    Processing,
    Confirmed,
}

public sealed record CheckoutForm(
    int Step,
    string Name,
    string Email,
    string Address,
    string Card,
    CheckoutStatus Status,
    Guid? OrderId) : IState<CheckoutForm>
{
    public static CheckoutForm Initial => new(1, "", "", "", "", CheckoutStatus.Editing, null);
}

/// <summary>Broadcast inside the "checkout" scope only (Route.Named("checkout", Broadcast)).</summary>
public sealed record OrderSubmitted(Guid OrderId, string Email, decimal Total) : IEvent;

/// <summary>Bubbles out of the checkout to anyone above it.</summary>
public sealed record OrderConfirmed(Guid OrderId, decimal Total, int Items, DateTime At) : IEvent;

// ── Live dashboard (complex) ─────────────────────────────────────────────

public sealed record PriceTick(string Symbol, decimal Price, decimal Change, DateTime At) : IEvent;

public sealed record MarketState(ImmutableDictionary<string, decimal> Prices, long Ticks, bool Running) : IState<MarketState>
{
    public static MarketState Initial => new(ImmutableDictionary<string, decimal>.Empty, 0, false);
}

// ── Render modes (complex) ───────────────────────────────────────────────

public sealed record IslandState(int Count) : IState<IslandState>
{
    public static IslandState Initial => new(0);
}

public sealed record IslandPinged(string From) : IEvent;
