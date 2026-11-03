namespace RForge.Blazor.Bus;

/// <summary>
/// A node in the scope tree. The root node is exposed as <see cref="IBus"/>; child scopes are created by
/// <see cref="EventScope"/> and <see cref="StateScope{TState}"/>.
/// </summary>
public interface IBusScope
{
    /// <summary>Optional name used by <see cref="ScopeTarget.Named"/> and <see cref="Route.Named"/>.</summary>
    string? Name { get; }

    /// <summary>The enclosing scope, or <see langword="null"/> for the root.</summary>
    IBusScope? Parent { get; }

    /// <summary>Whether this is the root bus.</summary>
    bool IsRoot { get; }

    /// <summary>
    /// Publishes an event. Completes when every receiving handler has completed.
    /// </summary>
    /// <exception cref="BusScopeNotFoundException">A named origin scope could not be found.</exception>
    /// <exception cref="BusReentrancyException">Nested publishes exceeded <see cref="BusOptions.MaxPublishDepth"/>.</exception>
    ValueTask PublishAsync<TEvent>(TEvent evt, Route route = default, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    /// <summary>
    /// Subscribes a non-component handler. The handler runs on the publisher's thread; dispose the result to unsubscribe.
    /// Components should use <see cref="SubscriptionBuilder"/> instead.
    /// </summary>
    /// <param name="handler">The handler to invoke for each delivered event.</param>
    /// <param name="at">The scope to attach to, relative to this scope.</param>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler, ScopeTarget at = default)
        where TEvent : IEvent;

    /// <summary>
    /// Resolves the store for <typeparamref name="TState"/> from the nearest provider at or above
    /// <paramref name="at"/>. The root implicitly provides every state type.
    /// </summary>
    IStateStore<TState> State<TState>(ScopeTarget at = default) where TState : IState<TState>;
}

/// <summary>
/// The root scope of a DI container. Registered as Scoped: one per circuit (Server),
/// one per app instance (WebAssembly), one per request (static rendering).
/// </summary>
public interface IBus : IBusScope;
