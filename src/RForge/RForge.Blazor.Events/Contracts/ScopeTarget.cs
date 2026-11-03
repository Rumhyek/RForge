namespace RForge.Blazor.Events;

/// <summary>How a scope is chosen relative to a starting scope.</summary>
public enum ScopeKind
{
    /// <summary>The starting scope itself: a component's closest enclosing scope (default).</summary>
    Nearest = 0,

    /// <summary>The root hub of the DI container.</summary>
    Root = 1,

    /// <summary>The nearest ancestor-or-self scope whose name matches.</summary>
    Named = 2,
}

/// <summary>Identifies a scope relative to a starting scope.</summary>
/// <param name="Kind">How the scope is chosen.</param>
/// <param name="Name">The scope name when <paramref name="Kind"/> is <see cref="ScopeKind.Named"/>.</param>
public readonly record struct ScopeTarget(ScopeKind Kind, string? Name = null)
{
    /// <summary>The starting scope itself. Equal to <c>default(ScopeTarget)</c>.</summary>
    public static ScopeTarget Nearest => default;

    /// <summary>The root hub.</summary>
    public static ScopeTarget Root => new(ScopeKind.Root);

    /// <summary>The nearest ancestor-or-self scope named <paramref name="name"/> (ordinal comparison).</summary>
    public static ScopeTarget Named(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new(ScopeKind.Named, name);
    }

    /// <inheritdoc />
    public override string ToString() => Kind == ScopeKind.Named ? $"Named(\"{Name}\")" : Kind.ToString();
}
