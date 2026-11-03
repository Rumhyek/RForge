# RForge.Blazor.Events: Design Specification

> **Status:** Draft · 2026-10-06
> **Target:** .NET 8, 9 and 10 (`[UseState]` needs C# 13)
> **Implementation:** source in `src/RForge/RForge.Blazor.Events`, with tests in `tests/` and examples in `samples/`. Decisions changed by implementation are recorded as ADR-016 and ADR-017.

## Purpose

Blazor components communicate easily along the parent/child chain (`[Parameter]`, `EventCallback`, `@bind`). Communication **across** the tree, between components that don't share a parent or are far apart, falls back to hand-rolled DI services with C# events. Those require every component to manage subscriptions, threading, re-rendering and cleanup by hand.

RForge.Blazor.Events provides **typed events and reactive state that any component can use, regardless of its position in the tree**, with the lifecycle chores handled automatically. The design adapts Svelte's stores and context to Blazor's DI and rendering model.

## Goals

1. **Tree-independent communication.** Any component can publish or react, whoever its parent is.
2. **Events and state.** Fire-and-forget messages and current-value state as separate, first-class primitives.
3. **Zero lifecycle boilerplate.** Subscribing, re-rendering, thread marshaling, error routing and disposal are automatic for components deriving from `RfEventComponent`.
4. **Opt-in scoping.** Subtrees can isolate events and own independent state instances, using Svelte's context + store pattern.
5. **Selective re-rendering.** Components re-render only when the state (or the projection of it) they use changes.
6. **Every render mode.** Static SSR, Interactive Server, WebAssembly and Auto, with or without prerendering, each working correctly within its own DI container.
7. **Trim/AOT safety.** No runtime reflection for wiring. A source generator does it at compile time.
8. **Compile-time feedback.** Analyzers turn misuse into build diagnostics.

## Non-goals

- **No JavaScript** and **no cross-container communication**. Interactive Server and WebAssembly islands on one page don't share a hub ([ADR-007](decisions.md#adr-007-no-js-and-no-cross-island-bridge)).
- **No cross-tab or cross-user messaging.**
- **No Redux model.** No reducers, actions-as-state-transitions or single state tree. Use Fluxor for that, alongside this library if needed.
- **No DevTools in v1.**
- **Not a replacement for parameters.** Parent ↔ direct child communication should keep using `[Parameter]` and `EventCallback`.

## Design principles

- **Explicit over magical, but automatic where it's safe.** Routes and scopes are explicit, and lifecycle plumbing is automatic.
- **The generator is optional sugar.** Everything it produces can be written by hand with the public API.
- **Cheap by default.** Fixed cascades, equality-suppressed notifications, selectors and no work in non-interactive renders.
- **Fail loudly at build time, safely at runtime.** Analyzers for misuse. Per-handler failure isolation at runtime.
- **Respect Blazor's model.** Use `InvokeAsync`, `DispatchExceptionAsync`, `RendererInfo` and `PersistentComponentState` instead of working around them.

## Glossary

| Term | Meaning |
|---|---|
| **Hub** | The root `IRfEventHub` of a DI container. Also used loosely for the whole system. |
| **Scope** | A node in the scope tree (`IRfEventHubScope`). The root is the hub. Children come from `<EventScope>`/`<StateScope>`. |
| **Nearest scope** | The closest enclosing scope of a component (root if none). |
| **Event** | An `IEvent` message: fire-and-forget, routed, no current value. |
| **State** | An `IState<TSelf>` value held in a store: current value, change-notified. |
| **Store** | `IStateStore<TState>`: holds one state instance and notifies subscribers on effective changes. |
| **Provider** | A scope that owns a store for a state type. Root implicitly provides every type. `StateScope` provides one. |
| **Handle** | A component's subscription to a store (`StateHandle`, `SelectorHandle`), owned and disposed by the component. |
| **Selector** | A static, pure function projecting a state to a smaller value. Re-renders happen only when the projection changes. |
| **Route** | Origin scope + direction (`Bubble`, `Local`, `Broadcast`) that determines which scopes receive an event. |
| **Scope target** | `Nearest`, `Root` or `Named(name)`: how a subscriber or route picks a scope. |
| **Island** | A region of a page with its own interactive render mode. |

## Architecture overview

```
RForge.Blazor.Events (NuGet package)
├── lib/{net8.0,net9.0,net10.0}/RForge.Blazor.Events.dll
│   ├── Contracts   IEvent, IState<T>, Route, ScopeTarget
│   ├── Runtime     IRfEventHub / IRfEventHubScope, IStateStore<T>, delivery engine
│   ├── Blazor      RfEventComponent, SubscriptionBuilder, handles, EventScope, StateScope
│   └── DI          AddRForgeEvents, RfEventOptions
└── analyzers/dotnet/cs/RForge.Blazor.Events.Generators.dll   (netstandard2.0)
    ├── Incremental source generator   [Subscribe], [UseState] → RegisterSubscriptions
    └── Analyzers RFEVT001–014
```

Repository layout:

```
src/
  RForge.Blazor.Events/                 runtime library (packs the generator)
  RForge.Blazor.Events.Generators/      generator + analyzers
tests/
  RForge.Blazor.Events.Tests/           runtime unit tests (bUnit for components)
  RForge.Blazor.Events.Generators.Tests/ generator snapshot + analyzer tests
samples/
  RForge.Blazor.Events.Sample/          Blazor Web App host (Server pages, static page, MarketFeed service)
  RForge.Blazor.Events.Sample.Client/   WebAssembly/Auto pages and shared message types
docs/
  design/                            this spec
```

### How a publish flows

```
Component.PublishAsync(e)            ──▶ resolve origin scope from Route.Origin
                                     ──▶ compute receiving scopes from Route.Direction
                                     ──▶ snapshot subscribers (scope order, then subscription order)
for each subscriber (sequential):
    component handler                ──▶ InvokeAsync → handler → StateHasChanged
                                         (exceptions → DispatchExceptionAsync)
    service handler                  ──▶ handler on the publisher's thread
                                         (exceptions → log or aggregate)
PublishAsync completes               ◀── after all handlers complete
```

### How a state change flows

```
store.Update(fn)                     ──▶ lock; new = fn(old); if equal → return
                                     ──▶ Value = new; Version++; unlock
for each handle:
    StateHandle                      ──▶ InvokeAsync → StateHasChanged
    SelectorHandle                   ──▶ InvokeAsync → select(latest) → changed? → StateHasChanged
    service callback                 ──▶ invoked synchronously
    CascadeValue / CascadeRootState  ──▶ cascade update → [CascadingParameter] consumers re-render
```

## Reading order

> Looking for how to *use* the library rather than why it's designed this way? Start with the [tutorials](../tutorials/README.md).

1. [api.md](api.md): public API surface (names used everywhere else)
2. [events.md](events.md): publishing, routing, delivery semantics
3. [state.md](state.md): stores, selectors, providers, compatibility mode, persistence
4. [scoping.md](scoping.md): `EventScope`, `StateScope`, fixed cascades, resolution
5. [components.md](components.md): `RfEventComponent` lifecycle and manual registration
6. [source-generator.md](source-generator.md): attributes, generated code, diagnostics
7. [render-modes.md](render-modes.md): behavior per render mode, limitations
8. [decisions.md](decisions.md): ADR log
9. [comparison.md](comparison.md): how this relates to the alternatives and to Svelte

## Open questions

Each has a proposed default that the spec currently assumes. Resolve them, and record the outcome as an ADR, before or during implementation.

| # | Question | Proposed default |
|---|---|---|
| 1 | **Polymorphic subscriptions:** does subscribing to `BaseEvent` receive `DerivedEvent`? | No in v1. Exact type match only (cheaper lookups, no surprises). |
| 2 | Does `Broadcast` include the publishing scope itself? | Yes. Origin, then descendants. |
| 3 | **Sequential vs. parallel** handler execution? | Sequential, deterministic ([ADR-015](decisions.md#adr-015-sequential-deterministic-delivery)). Parallel might become an opt-in per publish later. |
| 4 | Customizable selector equality in the attribute form? | Default comparer only for `[UseState]`. A custom comparer is available through the manual API. |
| 5 | Exact .NET 10 persistence API for root state, and its behavior with enhanced navigation? | Investigate `PersistentComponentState` vs. .NET 10 service-level persistence when implementing. |
| 6 | Persist `StateScope` state across prerender (keyed by `Name`)? | Not in v1. |
| 7 | Can one scope provide several state types (`<Provide TState=…/>` children)? | Not in v1. Nest `StateScope`s. |
| 8 | Subscribing after initialization (runtime `SubscriptionBuilder` use)? | Not in v1. Use `Scope.Subscribe` manually. |
| 9 | Default `HandlerErrorMode`: `Log`, or `Throw` in Development? | `Log` everywhere. |
| 10 | Is `Route.Global` worth keeping as a preset, or should it be spelled out as `new Route(ScopeTarget.Root, RouteDirection.Broadcast)`? | Keep it. "Everyone" is a common need. |
| 11 | Missing named scope: throw, or log and skip? | Throw `RfEventScopeNotFoundException`. |
| 12 | Derived state across stores (Svelte `derived` of several stores)? | Not in v1. Selectors project a single store. |

## Framework behaviors verified during implementation

The design depended on several framework behaviors. Results from implementing against the .NET 10 SDK (10.0.401):

| Behavior | Result |
|---|---|
| Generators can't see Razor's generated output; analyzers can identify Razor-originated members | ✅ Confirmed. `RFEVT001` fires on `@code` members and points at the `.razor` line ([source-generator.md](source-generator.md#code-behind-is-required)). |
| Base class can be declared in code-behind only | ❌ Wrong. Razor emits `: ComponentBase` without `@inherits`, so use `@inherits RfEventComponent` ([ADR-017](decisions.md#adr-017-base-class-goes-in-inherits)). |
| Skipping subscriptions during prerendering is safe | ❌ Wrong. It left stale prerendered HTML, so components now subscribe in every mode ([ADR-016](decisions.md#adr-016-subscribe-in-every-render-mode-supersedes-adr-014)). |
| `SetParametersAsync` + `ParameterView.Empty` with a private `[CascadingParameter]` on the base class | ✅ Works (all component tests and samples). |
| `RegisterOnPersisting` from a non-component service | ⚠️ Requires an explicit render mode in .NET 10. Uses `InteractiveAuto` ([state.md](state.md#persistence-across-prerendering)). |
| `CascadingValueSource<T>.NotifyChangedAsync` off the renderer thread | ✅ Works from store callbacks (tests and the plain-components sample). |
| The C# compiler skips analyzers when compilation errors exist | ⚠️ Confirmed. The generator stubs invalid `[UseState]` properties so analyzer errors stay visible ([source-generator.md](source-generator.md#invalid-usestate-properties-are-stubbed)). |
| Renderer disposal when a component implements both `IDisposable` and `IAsyncDisposable` | ❓ Not yet verified. `RFEVT014` stays a warning ([components.md](components.md#disposal)). |
