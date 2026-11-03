# State

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Bus design spec](index.md)

State is the bus's version of a **Svelte store**: a value that always exists, can be read at any time, and notifies subscribers when it changes. Use state when a component that appears *later* needs to know the current value. Use [events](events.md) when only the fact that something happened matters.

| | Event | State |
|---|---|---|
| Has a current value | No | Yes |
| Late subscriber sees past changes | No | Yes, reads `Value` |
| Change notification | Every publish | Only on an *effective* change (record equality) |
| Routing | `Route` (Bubble, Broadcast, …) | Nearest provider |

## Defining state

```csharp
public sealed record CartState(int Count, decimal Total) : IState<CartState>
{
    public static CartState Initial => new(0, 0m);
}
```

- `IState<TSelf>` requires `static abstract TSelf Initial`, so every state type defines its own starting value and no DI registration is needed. The compiler enforces this.
- Use a **`record`**. Change detection relies on `EqualityComparer<TState>.Default`, so records give value equality and `with` expressions for free. A class falls back to reference equality, which means every `Set` with a new instance notifies (analyzer `RFBUS013` warns).
- Treat state as **immutable**. Mutating a property of `Value` in place bypasses change detection entirely.

## Stores

Every provided state lives in an `IStateStore<TState>` (see [api.md](api.md#bus-and-scopes)):

```csharp
var store = State<CartState>();          // from BusComponentBase, or scope.State<CartState>()

var current = store.Value;
store.Set(current with { Count = 3 });
store.Update(s => s with { Count = s.Count + 1, Total = s.Total + price });
```

### Change semantics
- `Set(value)` is a **no-op** when `value` equals the current `Value`. No version bump, no notifications. This is the main protection against render storms.
- `Update(fn)` is an **atomic read-modify-write** under a per-store lock. On Blazor Server, two threads updating the same store at once can't lose updates.
- `Version` increments on every effective change.
- Notifications are sent **after** the lock is released, in version order.

### Why `Set` is synchronous
State writes are usually a direct reaction to UI input. Svelte's `store.set` is synchronous, and making writes awaitable adds friction without benefit: the value changes immediately either way. The **notifications** to components are asynchronous (queued through each component's `InvokeAsync`), and they always read the store's *latest* value when they run. Several quick writes therefore coalesce into one render with the final value.

## Consuming state in components

### Whole value

```csharp
public partial class CartBadge : BusComponentBase
{
    [UseState] private partial CartState Cart { get; }
}
```
```razor
<span class="badge">@Cart.Count</span>
```

The component re-renders whenever `CartState` effectively changes.

### Selectors

When a component depends on part of a state, a selector limits re-renders to changes in that part:

```csharp
public partial class CartCount : BusComponentBase
{
    [UseState(Select = nameof(SelectCount))]
    private partial int Count { get; }

    private static int SelectCount(CartState s) => s.Count;
}
```

- The selector runs on each store notification. The component re-renders only if the result differs from the previous result according to `EqualityComparer<TSelected>.Default`. A custom comparer can be supplied with the manual API.
- Selectors must be **static** and **pure**. The generator infers `TState` from the selector's parameter type (analyzer `RFBUS008`).
- A changed `Total` doesn't re-render `CartCount`. Neither Svelte context nor `CascadingValueSource<T>` can do this, and it's the main reason the design uses its own state mechanism instead of building directly on cascades ([ADR-006](decisions.md#adr-006-statescope-option-c)).

### Writing
Generated `[UseState]` properties are read-only. Writes go through the store:

```csharp
private void Add(Product p) =>
    State<CartState>().Update(s => s with { Count = s.Count + 1, Total = s.Total + p.Price });
```

When using the manual API, `StateHandle<T>.Set/Update` forward to the store.

### Non-component consumers

```csharp
var store = bus.State<CartState>();
using var sub = store.Subscribe(s => logger.LogInformation("Cart now {Count}", s.Count));
```

Store callbacks run **synchronously** on the writer's thread. They must be fast and must not touch UI.

## Where state lives: provider resolution

A state type is **provided** by a scope. Resolution walks up from the requesting scope:

1. Start at the target scope (`Nearest` by default, or `Root`/`Named` if specified).
2. If that scope provides `TState`, use its store.
3. Otherwise move to the parent and repeat.
4. At root: if no store exists for `TState`, **create one lazily** with `TState.Initial` (or the persisted value, see below).

So **every state type is implicitly provided at root**, and `<StateScope>` adds overriding providers lower in the tree:

```razor
@* Two independent checkout forms on one page *@
<StateScope TState="CheckoutForm">
    <CheckoutFormView />     @* sees form #1 *@
</StateScope>

<StateScope TState="CheckoutForm" Initial="@(new CheckoutForm("vip@example.com", false))">
    <CheckoutFormView />     @* sees form #2 *@
</StateScope>

<CartBadge />                @* CartState → root store *@
```

Resolution happens **once**, when the component registers its subscriptions. Scopes are fixed for a component's lifetime, so the resolved store never changes.

See [scoping.md](scoping.md) for `<StateScope>` details.

## Compatibility mode: `CascadeValue`

Components that don't derive from `BusComponentBase` (third-party components, or components you'd rather not change) can't hold handles. For them, a provider can **also** publish its value as an ordinary cascading value:

```razor
<StateScope TState="CheckoutForm" CascadeValue="true">
    <ThirdPartySummary />   @* [CascadingParameter] public CheckoutForm Form { get; set; } *@
    <CheckoutFormView />    @* BusComponentBase consumer: unaffected, still selective *@
</StateScope>
```

For root-level state, the equivalent is `options.CascadeRootState<CartState>()`. It registers a `CascadingValueSource<CartState>` through `AddCascadingValue` and calls `NotifyChangedAsync(newValue)` on each effective change.

**Costs, by design:**
- Every `[CascadingParameter]` consumer re-renders on every effective change. There are no selectors on this path.
- The cascaded value is read-only. Plain components that need to write must inject `IBus` (or receive the `IBusScope`) and use the store.
- In a `StateScope`, the extra `<CascadingValue>` is only rendered when `CascadeValue="true"`. Base-class consumers pay nothing when it's off.

## Persistence across prerendering

When a page prerenders and then becomes interactive, the interactive session starts with a **new DI scope** and so a new root bus. Without persistence, any state set during prerender (for example, data loaded in `OnInitializedAsync`) is lost and the UI flickers back to `Initial`.

Opt in per state type:

```csharp
builder.Services.AddBlazorBus(o => o
    .PersistState<CartState>()                          // reflection-based JSON
    .PersistState(AppJsonContext.Default.CheckoutForm)); // AOT/trim-safe JSON
```

Behavior:
- **During prerender:** the root bus registers a persisting callback with `PersistentComponentState`, serializing the current root `Value` of each opted-in state.
- **When the interactive session starts:** when a root store is first created lazily, it tries to take the persisted value before falling back to `Initial`.
- Only **root-provided** state is persisted in v1. `StateScope` state isn't, because there's no stable identity to match a prerendered scope to its interactive counterpart. See the [open questions](index.md#open-questions).
- State must be JSON-serializable. On WebAssembly with trimming/AOT, pass a source-generated `JsonTypeInfo<T>`. The reflection overload is annotated `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`.

**Implementation notes (verified on .NET 10):**
- The bus uses `PersistentComponentState.RegisterOnPersisting(callback, RenderMode.InteractiveAuto)`. A callback registered from outside a component **must** name a render mode, or .NET 10 throws *"must be associated with a component or define an explicit render mode"*. `InteractiveAuto` targets both the Server and WebAssembly state stores, so root state follows whichever runtime takes over. Both blobs show up in the prerendered page.
- Values are serialized to a JSON string by the bus, and only that `string` is handed to the framework. That keeps the framework's own (reflection-based) serializer trim-safe.
- Persistence keys are `RForge.Blazor.Bus:{TState.FullName}`.
- Not yet checked: interaction with enhanced navigation and with .NET 10's circuit pause/resume persistence.
