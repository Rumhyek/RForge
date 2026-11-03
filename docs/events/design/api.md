# Public API Sketch

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

This file is the **single source of truth for public names**. The other spec files describe behavior and refer back here. Signatures are illustrative: XML docs, nullability annotations and some overloads are left out. Exact shapes may still change during implementation, but any rename should be made here first.

All types live in the `RForge.Blazor.Events` namespace unless noted.

---

## Message contracts

```csharp
/// Marker for fire-and-forget messages. Prefer immutable records.
public interface IEvent { }

/// A piece of shared state with a well-defined starting value.
/// TSelf should be a record so value equality suppresses no-op notifications.
public interface IState<TSelf> where TSelf : IState<TSelf>
{
    static abstract TSelf Initial { get; }
}
```

Example domain used throughout the spec:

```csharp
public sealed record ItemAdded(int ProductId, int Quantity) : IEvent;
public sealed record CheckoutSubmitted(Guid OrderId) : IEvent;

public sealed record CartState(int Count, decimal Total) : IState<CartState>
{
    public static CartState Initial => new(0, 0m);
}

public sealed record CheckoutForm(string? Email, bool AddressValid) : IState<CheckoutForm>
{
    public static CheckoutForm Initial => new(null, false);
}
```

---

## Scope targeting and routing

```csharp
public enum ScopeKind
{
    Nearest = 0,   // the closest enclosing scope (default)
    Root    = 1,   // the root hub for this DI container
    Named   = 2,   // the nearest ancestor-or-self scope with a given Name
}

/// Identifies a scope relative to a starting point.
public readonly record struct ScopeTarget(ScopeKind Kind, string? Name = null)
{
    public static ScopeTarget Nearest { get; }       // == default
    public static ScopeTarget Root { get; }
    public static ScopeTarget Named(string name);
}

public enum RouteDirection
{
    Bubble    = 0, // origin scope, then each ancestor up to root (default)
    Local     = 1, // origin scope only
    Broadcast = 2, // origin scope and all its descendants (depth-first, pre-order)
}

/// Where a published event goes: an origin scope plus a direction.
public readonly record struct Route(ScopeTarget Origin, RouteDirection Direction)
{
    public static Route Bubble    { get; } // (Nearest, Bubble)    == default
    public static Route Local     { get; } // (Nearest, Local)
    public static Route Broadcast { get; } // (Nearest, Broadcast)
    public static Route Root      { get; } // (Root,    Local)     root subscribers only
    public static Route Global    { get; } // (Root,    Broadcast) every scope in the container
    public static Route Named(string name, RouteDirection direction = RouteDirection.Local);
}
```

`default(Route)` is `Route.Bubble`, and `default(ScopeTarget)` is `ScopeTarget.Nearest`. The enum zero values are chosen so that this holds.

---

## Hub and scopes

```csharp
/// A node in the scope tree. The root node is exposed as IRfEventHub.
public interface IRfEventHubScope
{
    string? Name { get; }
    IRfEventHubScope? Parent { get; }
    bool IsRoot { get; }

    ValueTask PublishAsync<TEvent>(
        TEvent evt,
        Route route = default,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    /// For non-component subscribers (services). Components use SubscriptionBuilder.
    IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, ValueTask> handler,
        ScopeTarget at = default)
        where TEvent : IEvent;

    /// Resolves the nearest provider of TState from this scope (root fallback).
    IStateStore<TState> State<TState>(ScopeTarget at = default)
        where TState : IState<TState>;
}

/// The root scope. Registered Scoped in DI: one per circuit (Server),
/// one per app (WebAssembly), one per request (static SSR).
public interface IRfEventHub : IRfEventHubScope { }
```

```csharp
public interface IStateStore<TState> where TState : IState<TState>
{
    TState Value { get; }
    long Version { get; }          // increments on every effective change
    IRfEventHubScope Owner { get; }       // the scope that provides this state

    void Set(TState value);                      // no-op if equal to Value
    void Update(Func<TState, TState> update);    // atomic read-modify-write

    /// Invoked synchronously on the thread that called Set/Update.
    IDisposable Subscribe(Action<TState> onChanged);
}
```

---

## Components

### `RfEventComponent`

```csharp
public abstract class RfEventComponent : ComponentBase, IAsyncDisposable
{
    // Infrastructure (non-public)
    [CascadingParameter] private IRfEventHubScope? CascadedScope { get; set; }
    [Inject] private IRfEventHub RootEventHub { get; set; } = default!;

    /// Nearest enclosing scope, or the root hub if none.
    protected IRfEventHubScope Scope { get; }

    /// Cancelled when the component is disposed.
    protected CancellationToken DisposalToken { get; }

    /// Override point used by the source generator (or by hand).
    protected virtual void RegisterSubscriptions(SubscriptionBuilder builder) { }

    protected ValueTask PublishAsync<TEvent>(
        TEvent evt, Route route = default, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    protected IStateStore<TState> State<TState>(ScopeTarget at = default)
        where TState : IState<TState>;

    public override Task SetParametersAsync(ParameterView parameters);

    /// Override instead of implementing IDisposable/IAsyncDisposable yourself.
    protected virtual ValueTask DisposeAsyncCore();
    public ValueTask DisposeAsync();
}
```

### `SubscriptionBuilder`

```csharp
public sealed class SubscriptionBuilder
{
    // Events: sync/async, with or without CancellationToken
    public void On<TEvent>(Action<TEvent> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent;
    public void On<TEvent>(Func<TEvent, Task> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent;
    public void On<TEvent>(Func<TEvent, ValueTask> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent;
    public void On<TEvent>(Func<TEvent, CancellationToken, Task> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent;
    public void On<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent;

    // State: whole value
    public StateHandle<TState> State<TState>(ScopeTarget at = default, bool render = true)
        where TState : IState<TState>;

    // State: projected value, re-renders only when the projection changes
    public SelectorHandle<TState, TSelected> State<TState, TSelected>(
        Func<TState, TSelected> selector,
        ScopeTarget at = default,
        bool render = true,
        IEqualityComparer<TSelected>? comparer = null)
        where TState : IState<TState>;
}
```

### Handles

```csharp
public sealed class StateHandle<TState> where TState : IState<TState>
{
    public TState Value { get; }                 // always reads the store's current value
    public IStateStore<TState> Store { get; }
    public void Set(TState value);              // forwards to Store
    public void Update(Func<TState, TState> update);
}

public sealed class SelectorHandle<TState, TSelected> where TState : IState<TState>
{
    public TSelected Value { get; }              // last selected value
    public IStateStore<TState> Store { get; }    // write through the store
}
```

Handles are owned by the component and disposed with it. They aren't `IDisposable` publicly, so users can't dispose them early by accident.

---

## Generator attributes

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SubscribeAttribute : Attribute
{
    public ScopeKind Scope { get; set; }        // default Nearest
    public string? ScopeName { get; set; }      // implies Scope = Named
    public bool Render { get; set; } = true;    // call StateHasChanged after handling
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class UseStateAttribute : Attribute
{
    public string? Select { get; set; }         // nameof(static selector method)
    public ScopeKind Scope { get; set; }
    public string? ScopeName { get; set; }
    public bool Render { get; set; } = true;
}
```

See [source-generator.md](source-generator.md) for the accepted signatures and the generated code.

---

## Razor components

```razor
@* Creates a child scope and cascades it (IsFixed) to its subtree *@
<EventScope Name="checkout">
    ...
</EventScope>

@* Creates a child scope that provides its own TState instance *@
<StateScope TState="CheckoutForm"
            Name="checkout-form"          @* optional *@
            Initial="@someValue"          @* optional; defaults to TState.Initial *@
            CascadeValue="true">          @* optional; also cascade TState for plain components *@
    ...
</StateScope>
```

| Component | Parameters |
|---|---|
| `EventScope` | `string? Name`, `RenderFragment ChildContent` |
| `StateScope<TState>` | `string? Name`, `TState? Initial`, `bool CascadeValue` (default `false`), `RenderFragment ChildContent` |

---

## Registration and options

Namespace: `Microsoft.Extensions.DependencyInjection`.

```csharp
public static class RfEventServiceCollectionExtensions
{
    public static IServiceCollection AddRForgeEvents(
        this IServiceCollection services,
        Action<RfEventOptions>? configure = null);
}

public sealed class RfEventOptions
{
    public int MaxPublishDepth { get; set; } = 16;
    public HandlerErrorMode HandlerErrorMode { get; set; } = HandlerErrorMode.Log;

    /// Persist root-level TState from prerender into the interactive session.
    /// Reflection-based JSON: not trim/AOT safe.
    [RequiresUnreferencedCode("…"), RequiresDynamicCode("…")]
    public RfEventOptions PersistState<TState>()
        where TState : IState<TState>;

    /// Same, with source-generated JSON metadata (trim/AOT safe).
    public RfEventOptions PersistState<TState>(JsonTypeInfo<TState> typeInfo)
        where TState : IState<TState>;

    /// Also expose root-level TState as a cascading value (CascadingValueSource<TState>)
    /// so plain components can use [CascadingParameter] TState.
    public RfEventOptions CascadeRootState<TState>()
        where TState : IState<TState>;
}

public enum HandlerErrorMode
{
    Log,    // log non-component handler failures and continue
    Throw,  // run all handlers, then throw AggregateException to the publisher
}
```

`AddRForgeEvents` must be called in **every** host that runs interactive components: the server `Program.cs` and, for WebAssembly or Auto, the client `Program.cs`.

---

## Exceptions

| Type | Thrown when |
|---|---|
| `RfEventScopeNotFoundException` | A `Named` route or subscription can't find a matching ancestor-or-self scope |
| `RfEventReentrancyException` | Nested publishes exceed `RfEventOptions.MaxPublishDepth` (probable event loop) |
| `ObjectDisposedException` | Publishing to, subscribing to, or reading state from a disposed scope |
