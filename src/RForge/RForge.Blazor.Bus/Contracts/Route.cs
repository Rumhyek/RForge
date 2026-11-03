namespace RForge.Blazor.Bus;

/// <summary>Which way an event travels from its origin scope.</summary>
public enum RouteDirection
{
    /// <summary>The origin scope, then each ancestor up to the root (default).</summary>
    Bubble = 0,

    /// <summary>The origin scope only.</summary>
    Local = 1,

    /// <summary>The origin scope and all of its descendants, depth-first in creation order.</summary>
    Broadcast = 2,
}

/// <summary>
/// Where a published event goes: an origin scope plus a direction.
/// <c>default(Route)</c> is <see cref="Bubble"/>.
/// </summary>
/// <param name="Origin">The scope delivery starts from, relative to the publisher's scope.</param>
/// <param name="Direction">Which way delivery proceeds from the origin.</param>
public readonly record struct Route(ScopeTarget Origin, RouteDirection Direction)
{
    /// <summary>The publisher's scope, then each ancestor up to the root.</summary>
    public static Route Bubble => default;

    /// <summary>The publisher's scope only.</summary>
    public static Route Local => new(ScopeTarget.Nearest, RouteDirection.Local);

    /// <summary>The publisher's scope and all of its descendants.</summary>
    public static Route Broadcast => new(ScopeTarget.Nearest, RouteDirection.Broadcast);

    /// <summary>Subscribers attached to the root bus only.</summary>
    public static Route Root => new(ScopeTarget.Root, RouteDirection.Local);

    /// <summary>Every scope in the container, starting at the root.</summary>
    public static Route Global => new(ScopeTarget.Root, RouteDirection.Broadcast);

    /// <summary>The nearest ancestor-or-self scope named <paramref name="name"/>.</summary>
    /// <param name="name">The scope name to look for.</param>
    /// <param name="direction">Which way to go from that scope. Defaults to that scope only.</param>
    public static Route Named(string name, RouteDirection direction = RouteDirection.Local) =>
        new(ScopeTarget.Named(name), direction);

    /// <inheritdoc />
    public override string ToString() => $"{Origin} → {Direction}";
}
