# Events

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

Events are **fire-and-forget messages**. They have no current value. A subscriber only receives events published while it's subscribed, and late subscribers miss earlier events. For "what is the value right now," use [state](state.md).

## Defining events

Any type that implements `IEvent` is an event. Use immutable `record` types: the same instance goes to every handler, so a mutable event is shared mutable state in disguise (analyzer `RFEVT012` flags this).

```csharp
public sealed record ItemAdded(int ProductId, int Quantity) : IEvent;
```

Events are **typed, not topic strings**. That gives compile-time checking, rename-safe refactoring, and "find all references" for both publishers and subscribers.

## Publishing

From a component deriving from [`RfEventComponent`](components.md):

```csharp
await PublishAsync(new ItemAdded(productId, 1));                    // Route.Bubble (default)
await PublishAsync(new CheckoutSubmitted(id), Route.Named("checkout"));
await PublishAsync(new ThemeChanged("dark"), Route.Global);
```

From anywhere else (services, plain components) with an injected `IRfEventHub` or a cascaded `IRfEventHubScope`:

```csharp
await hub.PublishAsync(new ItemAdded(productId, 1));
```

Publishing through the root `IRfEventHub` puts the origin at the root, so `Route.Bubble` from a service only reaches root subscribers. To reach into a subtree from outside it, use `Route.Global` or hold a reference to the subtree's `IRfEventHubScope`.

## Routing

A `Route` combines an **origin** (which scope to start from) with a **direction** (where to go from there). See [api.md](api.md#scope-targeting-and-routing).

| Preset | Origin | Direction | Receives |
|---|---|---|---|
| `Route.Bubble` *(default)* | Nearest | Bubble | Origin scope, then each ancestor up to root |
| `Route.Local` | Nearest | Local | Origin scope only |
| `Route.Broadcast` | Nearest | Broadcast | Origin scope and all descendants |
| `Route.Root` | Root | Local | Root subscribers only |
| `Route.Global` | Root | Broadcast | Every scope in the container |
| `Route.Named("x")` | Nearest ancestor-or-self named `x` | Local (overridable) | That scope only, by default |

### Worked example

```
Root
├── EventScope "checkout"            (A)
│   ├── EventScope "address"         (B)   ◀── publisher lives here
│   │   └── StateScope<CheckoutForm> (C)
│   └── EventScope "payment"         (D)
└── EventScope "sidebar"             (E)
```

The publisher's nearest scope is **B**:

| Route | Scopes whose subscribers receive it, in order |
|---|---|
| `Bubble` | B → A → Root |
| `Local` | B |
| `Broadcast` | B → C |
| `Root` | Root |
| `Global` | Root → A → B → C → D → E |
| `Named("checkout")` | A |
| `Named("checkout", Broadcast)` | A → B → C → D |
| `Named("sidebar")` | ❌ `RfEventScopeNotFoundException`: E is not an ancestor of B |

**Why `Bubble` is the default:** it matches DOM intuition ("something happened in here, anyone above me who cares can react"). Root subscribers always receive bubbled events, so a component that subscribes at root behaves like a plain global hub no matter where the publisher sits. See [ADR-005](decisions.md#adr-005-bubble-is-the-default-route).

### Which scope a subscriber listens on

A subscription attaches to exactly **one** scope, chosen by its `ScopeTarget` relative to the subscriber's position:

| `ScopeTarget` | Attaches to |
|---|---|
| `Nearest` *(default)* | The subscriber's nearest enclosing scope (root if none) |
| `Root` | The root hub |
| `Named("x")` | The nearest ancestor-or-self scope named `x`, or `RfEventScopeNotFoundException` |

A subscription receives an event when its scope is in the route's receiving set. For example, a component inside **C** that subscribes with `Nearest` receives `Broadcast` from B but **not** `Bubble` from B, because Bubble goes upward and C is below B.

Scopes created by `<StateScope>` are ordinary scopes for routing purposes. A component nested in a `StateScope` has that `StateScope` as its *nearest* scope. See [scoping.md](scoping.md#statescope-is-a-scope).

## Delivery semantics

### Ordering
- Handlers run **sequentially**, and each one is awaited before the next starts.
- Order is **deterministic**: scopes in route order (table above), and within a scope, handlers in subscription order.
- `Broadcast` traverses depth-first, pre-order, with child scopes in creation order.

Sequential delivery is the predictable choice for UI work. Parallel delivery is an [open question](index.md#open-questions).

### Awaiting
`PublishAsync` completes when **every** receiving handler has completed, including component handlers marshaled through `InvokeAsync`. A publisher can therefore rely on "all reactions have run" after the `await`.

For component handlers, "completed" means the handler body finished and a re-render was **queued**, not that the render was applied to the DOM.

### Failure isolation
A failing handler **never** stops delivery to the remaining handlers.

| Handler kind | On exception |
|---|---|
| Component (`RfEventComponent`) | Routed to that component's `DispatchExceptionAsync`, so the nearest `ErrorBoundary` handles it as if the component had thrown during rendering. The publisher doesn't see it. |
| Non-component (`IRfEventHubScope.Subscribe`) | `HandlerErrorMode.Log` (default): logged via `ILogger<IRfEventHub>`. `HandlerErrorMode.Throw`: after all handlers run, the publisher gets an `AggregateException`. |

### Reentrancy
A handler may publish further events. Nested publishes run **inline**: the nested delivery completes before the outer delivery moves to its next handler. This keeps causality obvious in stack traces.

Publish depth is tracked with an `AsyncLocal<int>`. Going past `RfEventOptions.MaxPublishDepth` (default 16) throws `RfEventReentrancyException` at the nested publish site, which is almost always an A→B→A event loop.

### Cancellation
- The `CancellationToken` passed to `PublishAsync` stops delivery to **remaining** handlers and is forwarded to handlers that accept a token.
- Component handlers receive a token linked to **both** the publish token and the component's `DisposalToken`, so a handler still running when its component is disposed gets cancelled.
- Cancellation surfaces to the publisher as `OperationCanceledException`.

### Subscriptions added or removed during delivery
The receiving set is **snapshotted** when a publish starts:
- A handler subscribed during delivery doesn't receive the in-flight event.
- A handler unsubscribed (or whose component is disposed) during delivery is **skipped** if it hasn't run yet.

## Subscribing

### Components (preferred)
With the generator (see [source-generator.md](source-generator.md)):

```csharp
public partial class CartBadge : RfEventComponent
{
    [Subscribe]
    private void OnItemAdded(ItemAdded e) => _pulse = true;
}
```

By hand (see [components.md](components.md#manual-registration)):

```csharp
protected override void RegisterSubscriptions(SubscriptionBuilder b)
{
    b.On<ItemAdded>(e => _pulse = true);
}
```

Component subscriptions are removed automatically on disposal, wrapped in `InvokeAsync`, and followed by `StateHasChanged` unless `render: false`.

### Services and other non-components

```csharp
public sealed class AnalyticsListener : IDisposable
{
    private readonly IDisposable _sub;

    public AnalyticsListener(IRfEventHub hub) =>
        _sub = hub.Subscribe<ItemAdded>(async (e, ct) => await TrackAsync(e, ct));

    public void Dispose() => _sub.Dispose();
}
```

Non-component handlers run on whatever thread is publishing. No `InvokeAsync` is applied, because no renderer is involved. Disposing the returned `IDisposable` is the caller's job.

> Services that depend on `IRfEventHub` must be **Scoped** (or Transient). A Singleton holding an `IRfEventHub` is a captive dependency, and on Blazor Server it would leak one user's circuit into all users. DI scope validation catches this in Development. See [render-modes.md](render-modes.md#di-lifetime).
