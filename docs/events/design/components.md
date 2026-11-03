# Component Integration

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

`RfEventComponent` is what makes the hub feel like Svelte's `$store`: subscribe, re-render, marshal threads, route errors and clean up, all without code from the component author. The [source generator](source-generator.md) only writes the subscription list. The base class handles everything else.

## Responsibilities

| Concern | Handled by `RfEventComponent` |
|---|---|
| Finding the nearest scope | Private `[CascadingParameter] IRfEventHubScope?`, falling back to injected `IRfEventHub` |
| Registering subscriptions | Calls `RegisterSubscriptions(builder)` once, at the right lifecycle point |
| Thread marshaling | Every component handler and state notification runs inside `InvokeAsync` |
| Re-rendering | `StateHasChanged()` after each handler/notification (unless `render: false`) |
| Error routing | Handler exceptions go to `DispatchExceptionAsync` so `ErrorBoundary` works |
| Cancellation | `DisposalToken`, linked into every handler's token |
| Cleanup | All subscriptions and handles released in `DisposeAsync` |
| Non-interactive rendering | Subscribes normally, so prerendered HTML reflects state loaded later in the same render ([ADR-016](decisions.md#adr-016-subscribe-in-every-render-mode-supersedes-adr-014)) |

## Lifecycle

### When registration happens

Registration must happen:
- **after** cascading parameters are assigned (the scope must be known), and
- **before** the user's `OnInitialized{Async}` runs, so generated `[UseState]` properties are readable there and handlers are live by the time initialization logic might publish.

It also **must not depend on the user calling `base.OnInitialized()`**. Forgetting that call is common and would silently break every subscription.

So `RfEventComponent` overrides `SetParametersAsync`:

```csharp
public override Task SetParametersAsync(ParameterView parameters)
{
    if (!_registered)
    {
        parameters.SetParameterProperties(this);   // assigns [Parameter] + [CascadingParameter]
        Register();                                  // resolve scope, call RegisterSubscriptions
        _registered = true;
        return base.SetParametersAsync(ParameterView.Empty); // runs OnInitialized, OnParametersSet, …
    }
    return base.SetParametersAsync(parameters);
}
```

Passing `ParameterView.Empty` to the base on the first call is a known Blazor pattern. Parameters have already been assigned, and the base then runs the normal lifecycle.

> If a derived component overrides `SetParametersAsync` itself, it must call `base.SetParametersAsync(parameters)`, as with any `ComponentBase`.

### Resulting order (first render)

1. Cascading and regular parameters assigned
2. Scope resolved, then `RegisterSubscriptions(builder)` runs and state handles are created
3. `OnInitialized` / `OnInitializedAsync`, where `[UseState]` values are readable
4. `OnParametersSet` / `OnParametersSetAsync`
5. Render

### Non-interactive rendering (static SSR and prerender)

Components register their subscriptions in **every** render mode, including static SSR and prerendering:
- **Prerendered HTML stays correct.** The static renderer keeps rendering until async initialization settles. If one component loads data after an `await` in `OnInitializedAsync` and writes it to a store, components that rendered *earlier* are notified and re-render before the HTML is sent. (An earlier draft skipped subscriptions when `RendererInfo.IsInteractive` was `false`. The checkout sample showed that this left an empty cart in the prerendered HTML. See [ADR-016](decisions.md#adr-016-subscribe-in-every-render-mode-supersedes-adr-014).)
- **No leaks or double delivery.** A prerender runs in its own request scope with its own root hub. Its components, subscriptions and hub are all disposed when the request ends, and the interactive instance subscribes to a different hub.
- Once a static response has been sent, nothing re-renders. Changes after that point have no visible effect, which is inherent to static rendering.

See [render-modes.md](render-modes.md).

## Handler execution

For each delivered event, the base class does roughly this:

```csharp
Task Deliver(TEvent e, CancellationToken publishToken) => InvokeAsync(async () =>
{
    if (_disposed) return;
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(publishToken, DisposalToken);
    try
    {
        await handler(e, linked.Token);
    }
    catch (OperationCanceledException) when (DisposalToken.IsCancellationRequested)
    {
        return; // component went away; not an error
    }
    catch (Exception ex)
    {
        await DispatchExceptionAsync(ex);
        return;
    }
    if (render) StateHasChanged();
});
```

Notes:
- **`InvokeAsync`** is needed on Blazor Server, where the publisher may be on a thread-pool thread. When the publisher is already on the renderer's sync context, `InvokeAsync` runs inline, so there's no extra hop.
- **Render coalescing.** Several handlers or state notifications on the same component during one dispatch each call `StateHasChanged`. Blazor already collapses these into a single queued render, so the base class doesn't batch further.
- **`render: false`** (`[Subscribe(Render = false)]`) is for handlers that only update fields not used in markup, or that call `StateHasChanged` themselves.
- State notifications follow the same path. Selector handles compare the new projection first and skip `StateHasChanged` when it's unchanged.

## Error handling

Handler exceptions go to `ComponentBase.DispatchExceptionAsync` (.NET 8+), so they behave as if the component threw during rendering:
- Inside an `ErrorBoundary`: the boundary shows its error content.
- Without one: the normal unhandled-error behavior for the host (circuit termination on Server, the error UI on WebAssembly).

The publisher is **never** affected by a component handler's failure. See [events.md](events.md#failure-isolation).

## Disposal

`RfEventComponent` implements `IAsyncDisposable`:

```csharp
public async ValueTask DisposeAsync()
{
    if (_disposed) return;
    _disposed = true;
    _disposalCts.Cancel();
    _subscriptions.Dispose();     // events + state handles
    await DisposeAsyncCore();     // user cleanup
    _disposalCts.Dispose();
    GC.SuppressFinalize(this);
}
```

Derived components **override `DisposeAsyncCore`** for their own cleanup instead of implementing `IDisposable`/`IAsyncDisposable` again. Analyzer `RFEVT014` warns when a derived component implements either interface directly, because Blazor calls only one of them and the base class's cleanup could be skipped.

> **Still to verify:** which disposal interface the .NET 10 renderer calls when a component implements both, and whether a re-implemented interface in a derived type can bypass the base implementation. `RFEVT014` ships as a warning until that's confirmed.

## Manual registration

The generator is a convenience. Everything it does can be written by hand, which also suits dynamic cases (subscribing to an event type chosen at runtime, conditional subscriptions):

```csharp
public class CartBadge : RfEventComponent
{
    private StateHandle<CartState> _cart = default!;
    private SelectorHandle<CartState, int> _count = default!;
    private bool _pulse;

    protected override void RegisterSubscriptions(SubscriptionBuilder b)
    {
        base.RegisterSubscriptions(b);

        _cart  = b.State<CartState>();
        _count = b.State<CartState, int>(s => s.Count);

        b.On<ItemAdded>(e => _pulse = true);
        b.On<CheckoutSubmitted>(async (e, ct) => await ResetAsync(ct), ScopeTarget.Named("checkout"));
    }
}
```

Manual and generated registration can coexist through the generator's `partial void OnRegisterSubscriptions(SubscriptionBuilder)` hook. See [source-generator.md](source-generator.md#combining-with-manual-registration).

`SubscriptionBuilder` is only valid during `RegisterSubscriptions`. Calling it afterward throws `InvalidOperationException`. Runtime subscriptions after initialization are an [open question](index.md#open-questions). Until it's resolved, use `Scope.Subscribe(...)` and dispose the result in `DisposeAsyncCore`, with the caveat that you then handle `InvokeAsync` yourself.

## Publishing and state helpers

```csharp
protected ValueTask PublishAsync<TEvent>(TEvent evt, Route route = default, CancellationToken ct = default);
protected IStateStore<TState> State<TState>(ScopeTarget at = default);
protected IRfEventHubScope Scope { get; }
protected CancellationToken DisposalToken { get; }
```

`PublishAsync` and `State` are relative to `Scope`, the component's nearest scope. These helpers don't subscribe or re-render. `State<T>()` is for **writing** (or a one-off read). For reactive reads, use `[UseState]` or a `StateHandle`.

## Using the hub without the base class

Supported, with fewer guarantees:

| Approach | What you get | What you handle yourself |
|---|---|---|
| `[Inject] IRfEventHub` + `Subscribe` | Global pub/sub | `InvokeAsync`, `StateHasChanged`, disposal, error routing; no scope awareness |
| `[CascadingParameter] IRfEventHubScope?` + `Subscribe` | Scope-aware pub/sub | Same as above |
| `[CascadingParameter] TState` with `CascadeValue="true"` / `CascadeRootState<T>()` | Reactive state reads, no code | Writes via `IRfEventHub`; re-renders on every change |

This is the escape hatch for third-party components and for teams that can't use a shared base class.
