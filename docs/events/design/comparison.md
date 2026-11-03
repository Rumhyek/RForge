# Comparison with Alternatives

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

How RForge.Blazor.Events relates to what Blazor developers can already use, with Svelte, the main design inspiration, as a reference point. The aim is an honest map of trade-offs, not a sales pitch. Some of these tools are better choices for some apps.

## Feature matrix

| | Plain DI + C# `event` | BlazorComponentBus | `CascadingValueSource<T>` | Fluxor | **RForge.Blazor.Events** | *Svelte (reference)* |
|---|---|---|---|---|---|---|
| **Model** | Hand-rolled | Pub/sub events | Reactive cascaded value | Redux: actions → reducers → state, plus effects | Events + state, both first-class | Stores / runes + context |
| **Events** | Yes | Yes | No | Actions act as events | Yes | Callbacks / DOM events |
| **State with current value** | Hand-rolled | No | Yes | Yes | Yes | Yes |
| **Auto re-render** | No | No | Yes, every consumer | Yes (`FluxorComponent`) | Yes, subscribers only | Yes, fine-grained |
| **Selectors / partial re-render** | No | N/A | No | Yes (selector APIs) | Yes (`[UseState(Select=…)]`) | Yes (derived) |
| **Auto unsubscribe** | No | No | Yes (framework) | Yes (base class) | Yes (base class) | Yes |
| **Thread marshaling** | Manual | Manual | Handled | Handled | Handled | N/A |
| **Errors reach `ErrorBoundary`** | Manual | Manual | N/A | Varies | Yes (`DispatchExceptionAsync`) | N/A |
| **Subtree scoping** | No | No | Yes (nest `CascadingValue`) | No, global per DI scope | Yes (`EventScope`, routes) | Yes (context) |
| **Multiple instances of one state** | Manual | N/A | Yes | No, one per feature | Yes (`StateScope`) | Yes |
| **Server ↔ WASM islands** | No | No | No | No | No (by design) | N/A, one JS realm |
| **Prerender state carry-over** | Manual | No | Manual | Manual | Opt-in (`PersistState<T>`) | SvelteKit handles it |
| **Trim/AOT friendly** | Yes | Mostly | Yes | Reflection-heavy (assembly scanning) | Yes (source generator) | N/A |
| **Ceremony per feature** | Medium | Low | Low | High (action + reducer + feature) | Low (record + attribute) | Very low |
| **Works without a base class** | Yes | Yes | Yes | No (for auto-subscribe) | Partially ([compat paths](components.md#using-the-hub-without-the-base-class)) | N/A |
| **Tooling** | None | None | None | Redux DevTools, middleware | Analyzers (code fixes planned) | Svelte DevTools |
| **Maturity** | N/A | Small, stable | Built in (.NET 8+) | Mature, widely used | Pre-release (0.0.1-rc4) | Mature |

## Notes on each

### Plain DI service + C# events
What everyone writes first. RForge.Blazor.Events is, at its core, this pattern with the five manual chores automated: unsubscribe, `InvokeAsync`, `StateHasChanged`, error routing and current-value semantics. Scoping and selectors come on top. **Choose plain DI** for one or two shared values in a small app.

### BlazorComponentBus
The closest in spirit to this library's event side: lightweight typed pub/sub through a DI service. It shows the idea is useful, but leaves lifecycle, threading and re-rendering to every component, and has no state or scoping. RForge.Blazor.Events is functionally a superset.

### `CascadingValueSource<T>` (built in, .NET 8+)
The strongest built-in alternative for **state**. Registered with `AddCascadingValue`, consumed with `[CascadingParameter]`, updated with `NotifyChangedAsync`. Its limits:
- every consumer re-renders on every notification
- no events
- root-level only (subtree cascades update only when their owner re-renders)

**Choose it** for scoped or global state on small trees where you'd rather not depend on a library. RForge.Blazor.Events uses it internally for `CascadeRootState<T>()` ([ADR-006](decisions.md#adr-006-statescope-option-c)).

### Fluxor
Solves a different problem: **predictable, centralized state transitions** with tooling (time-travel debugging, action logs, middleware). RForge.Blazor.Events doesn't compete there: it has no reducers, no single state tree and no DevTools. Fluxor, in turn, has no subtree scoping, no multiple instances of a feature state, and significant ceremony for "tell that component something happened."

**They can coexist:** Fluxor for domain state and auditability, RForge.Blazor.Events for UI-level, tree-shaped communication.

### Svelte (reference)
The model being adapted. The direct mapping:

| Svelte | RForge.Blazor.Events |
|---|---|
| `writable()` store / `$state` module | `IState<T>` + `IStateStore<T>` |
| `$store` auto-subscription | `[UseState]` + `RfEventComponent` |
| `derived()` | Selectors (projection only, no cross-store derivation in v1) |
| `setContext` / `getContext` | `<EventScope>` / `<StateScope>` + cascaded `IRfEventHubScope` |
| Context holding a store | `<StateScope TState="…">` |
| Callback props / DOM events | `[Parameter]` + `EventCallback` (unchanged; not the hub's job) |

What Svelte gets for free and Blazor doesn't, and therefore what this library exists to add: automatic unsubscription, automatic re-render scheduling, a current value for late subscribers, and a single JS realm with no threading. The last one has no equivalent here, which is why cross-island communication is a non-goal.

## What's actually new here

Most of the library is a **better-packaged version of existing patterns**. Two things have no direct equivalent in the Blazor ecosystem:

1. **Scoped event routing** (`Bubble`, `Broadcast`, `Named`, `Global`) combined with **scoped, multi-instance state** in one model.
2. **Selective re-rendering for scoped state.** Cascades can scope but not select, and Fluxor can select but not scope.

Everything else (lifecycle automation, typed events, generator ergonomics) is about quality of life, not new capability.

## Known costs relative to alternatives

- **Base class requirement** for the full experience, as with Fluxor, and unlike `CascadingValueSource<T>`.
- **Code-behind requirement** for annotated members ([ADR-004](decisions.md#adr-004-annotated-members-require-code-behind)). No alternative has this.
- **No DevTools.** A runtime event log/inspector is a natural future addition.
- **Maintenance surface:** a runtime library plus a generator and analyzers, kept in sync across .NET releases.
