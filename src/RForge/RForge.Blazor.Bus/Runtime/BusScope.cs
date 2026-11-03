using Microsoft.Extensions.Logging;

namespace RForge.Blazor.Bus;

/// <summary>
/// A node in the scope tree. All scopes of one tree share a single lock (<see cref="_gate"/>), which guards
/// the tree shape, subscription lists and provider maps. Handlers are never invoked while holding it.
/// </summary>
internal class BusScope : IBusScope, IDisposable
{
    private readonly object _gate;
    private readonly List<BusScope> _children = [];
    private readonly Dictionary<Type, List<EventSubscription>> _subscriptions = [];
    private readonly Dictionary<Type, IDisposable> _providers = [];
    private bool _disposed;

    private protected BusScope(RootBus? root, BusScope? parent, string? name)
    {
        Root = root ?? (RootBus)this;
        ParentScope = parent;
        Name = name;
        _gate = parent?._gate ?? new object();
    }

    public string? Name { get; }

    public IBusScope? Parent => ParentScope;

    public bool IsRoot => ParentScope is null;

    internal BusScope? ParentScope { get; }

    internal RootBus Root { get; }

    internal bool IsDisposed
    {
        get { lock (_gate) return _disposed; }
    }

    internal static BusScope From(IBusScope scope) =>
        scope as BusScope ?? throw new InvalidOperationException(
            $"Scope type '{scope.GetType().FullName}' is not supported. Scopes must be created by EventScope, StateScope or the root IBus.");

    // ── Tree ─────────────────────────────────────────────────────────────

    internal BusScope CreateChild(string? name)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var child = new BusScope(Root, this, name);
            _children.Add(child);
            return child;
        }
    }

    internal BusScope Resolve(ScopeTarget target) => target.Kind switch
    {
        ScopeKind.Nearest => this,
        ScopeKind.Root => Root,
        ScopeKind.Named => FindNamed(target.Name ?? throw new ArgumentException("A named scope target requires a name.", nameof(target)))
                           ?? throw new BusScopeNotFoundException(target.Name),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target.Kind, "Unknown scope kind."),
    };

    private BusScope? FindNamed(string name)
    {
        for (var scope = this; scope is not null; scope = scope.ParentScope)
        {
            if (string.Equals(scope.Name, name, StringComparison.Ordinal))
            {
                return scope;
            }
        }

        return null;
    }

    // ── Events ───────────────────────────────────────────────────────────

    public async ValueTask PublishAsync<TEvent>(TEvent evt, Route route = default, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(evt);
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var origin = Resolve(route.Origin);
        var receivers = origin.CollectReceivers<TEvent>(route.Direction);
        if (receivers.Count == 0)
        {
            return;
        }

        var options = Root.Options;
        using var depth = PublishDepth.Enter(options.MaxPublishDepth, typeof(TEvent));

        List<Exception>? errors = null;
        foreach (var receiver in receivers)
        {
            if (receiver.IsDisposed)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await receiver.Handler(evt, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (options.HandlerErrorMode == HandlerErrorMode.Throw)
                {
                    (errors ??= []).Add(ex);
                }
                else
                {
                    Root.Logger.LogError(ex, "A handler for event {EventType} threw an exception.", typeof(TEvent).FullName);
                }
            }
        }

        if (errors is not null)
        {
            throw new AggregateException($"One or more handlers for event {typeof(TEvent).FullName} threw an exception.", errors);
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler, ScopeTarget at = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        var target = Resolve(at);
        var subscription = new EventSubscription<TEvent>(target, handler);
        target.AddSubscription(typeof(TEvent), subscription);
        return subscription;
    }

    private void AddSubscription(Type eventType, EventSubscription subscription)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_subscriptions.TryGetValue(eventType, out var list))
            {
                _subscriptions[eventType] = list = [];
            }

            list.Add(subscription);
        }
    }

    internal void RemoveSubscription(Type eventType, EventSubscription subscription)
    {
        lock (_gate)
        {
            if (_subscriptions.TryGetValue(eventType, out var list))
            {
                list.Remove(subscription);
            }
        }
    }

    /// <summary>Snapshots the receivers for a publish, in delivery order.</summary>
    private List<EventSubscription<TEvent>> CollectReceivers<TEvent>(RouteDirection direction)
    {
        var receivers = new List<EventSubscription<TEvent>>();
        lock (_gate)
        {
            switch (direction)
            {
                case RouteDirection.Local:
                    AppendSubscriptions(this, receivers);
                    break;

                case RouteDirection.Bubble:
                    for (var scope = this; scope is not null; scope = scope.ParentScope)
                    {
                        AppendSubscriptions(scope, receivers);
                    }

                    break;

                case RouteDirection.Broadcast:
                    AppendDescendants(this, receivers);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown route direction.");
            }
        }

        return receivers;

        static void AppendDescendants(BusScope scope, List<EventSubscription<TEvent>> receivers)
        {
            AppendSubscriptions(scope, receivers);
            foreach (var child in scope._children)
            {
                AppendDescendants(child, receivers);
            }
        }

        static void AppendSubscriptions(BusScope scope, List<EventSubscription<TEvent>> receivers)
        {
            if (scope._subscriptions.TryGetValue(typeof(TEvent), out var list))
            {
                foreach (var subscription in list)
                {
                    receivers.Add((EventSubscription<TEvent>)subscription);
                }
            }
        }
    }

    // ── State ────────────────────────────────────────────────────────────

    public IStateStore<TState> State<TState>(ScopeTarget at = default) where TState : IState<TState>
    {
        ThrowIfDisposed();
        var start = Resolve(at);

        lock (_gate)
        {
            for (var scope = start; scope is not null; scope = scope.ParentScope)
            {
                if (scope._providers.TryGetValue(typeof(TState), out var store))
                {
                    return (StateStore<TState>)store;
                }
            }

            // The root implicitly provides every state type.
            BusScope root = Root;
            var created = Root.CreateRootStore<TState>();
            root._providers.Add(typeof(TState), created);
            return created;
        }
    }

    /// <summary>Makes this scope the provider of <typeparamref name="TState"/> for its subtree.</summary>
    internal StateStore<TState> Provide<TState>(TState initial) where TState : IState<TState>
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_providers.ContainsKey(typeof(TState)))
            {
                throw new InvalidOperationException($"This scope already provides {typeof(TState).Name}.");
            }

            var store = new StateStore<TState>(this, initial);
            _providers.Add(typeof(TState), store);
            return store;
        }
    }

    /// <summary>Returns the store for <typeparamref name="TState"/> if this scope already provides it.</summary>
    internal StateStore<TState>? TryGetProvidedStore<TState>() where TState : IState<TState>
    {
        lock (_gate)
        {
            return _providers.TryGetValue(typeof(TState), out var store) ? (StateStore<TState>)store : null;
        }
    }

    // ── Lifetime ─────────────────────────────────────────────────────────

    public virtual void Dispose()
    {
        BusScope[] children;
        EventSubscription[] subscriptions;
        IDisposable[] providers;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ParentScope?._children.Remove(this);

            children = [.. _children];
            subscriptions = [.. _subscriptions.Values.SelectMany(static list => list)];
            providers = [.. _providers.Values];

            _children.Clear();
            _subscriptions.Clear();
            _providers.Clear();
        }

        foreach (var child in children)
        {
            child.Dispose();
        }

        foreach (var subscription in subscriptions)
        {
            subscription.MarkDisposed();
        }

        foreach (var provider in providers)
        {
            provider.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    public override string ToString() => IsRoot ? "Root" : $"Scope({Name ?? "unnamed"})";
}
