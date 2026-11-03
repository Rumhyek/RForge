# Decision Log

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

Architecture decisions in lightweight ADR form. Each records the context, the decision, the alternatives considered and the consequences, so the reasoning isn't lost when the code is the only artifact left.

**Statuses:**
- **Accepted:** agreed during design discussion.
- **Proposed:** introduced while writing the spec to fill a gap. Implemented as written, but still open to change.
- **Superseded:** replaced by a later ADR. The original text is kept for history.

To change a decision, add a new ADR that supersedes it rather than rewriting history.

| ADR | Title | Status |
|---|---|---|
| [001](#adr-001-di-based-hub-rather-than-cascade-only) | DI-based hub rather than cascade-only | Accepted |
| [002](#adr-002-events-and-state-are-separate-primitives) | Events and state are separate primitives | Accepted |
| [003](#adr-003-base-class--source-generator-hybrid) | Base class + source generator hybrid | Accepted |
| [004](#adr-004-annotated-members-require-code-behind) | Annotated members require code-behind | Accepted |
| [005](#adr-005-bubble-is-the-default-route) | `Bubble` is the default route | Accepted |
| [006](#adr-006-statescope-option-c) | StateScope: Option C | Accepted |
| [007](#adr-007-no-js-and-no-cross-island-bridge) | No JS and no cross-island bridge | Accepted |
| [008](#adr-008-net-10-minimum) | .NET 10 minimum | Accepted |
| [009](#adr-009-record-equality-for-change-detection) | Record equality for change detection | Accepted |
| [010](#adr-010-the-root-hub-is-scoped) | The root hub is Scoped | Accepted |
| [011](#adr-011-scope-objects-are-cascaded-with-isfixed) | Scope objects are cascaded with `IsFixed` | Accepted |
| [012](#adr-012-routes-are-origin--direction) | Routes are origin + direction | Proposed |
| [013](#adr-013-statescope-creates-its-own-scope) | `StateScope` creates its own scope | Proposed |
| [014](#adr-014-registration-in-setparametersasync-skipped-when-non-interactive) | Registration in `SetParametersAsync`, skipped when non-interactive | Superseded by 016 |
| [015](#adr-015-sequential-deterministic-delivery) | Sequential, deterministic delivery | Proposed |
| [016](#adr-016-subscribe-in-every-render-mode-supersedes-adr-014) | Subscribe in every render mode | Accepted (found during implementation) |
| [017](#adr-017-base-class-goes-in-inherits) | Base class goes in `@inherits` | Accepted (found during implementation) |

---

## ADR-001: DI-based hub rather than cascade-only

**Status:** Accepted

**Context.** Components anywhere in the tree must communicate regardless of their parent. Blazor offers two tree-independent channels: DI services and (root-level) cascading values.

**Decision.** The core is a **DI-registered hub** (`IRfEventHub`). Cascades are used only to carry *scope identity* (see ADR-011), never as the main change-notification mechanism.

**Alternatives.**
- *Cascade-only (`CascadingValueSource<T>` / `CascadingValue`):* built in and needs no base class, but has no events, re-renders every consumer on every change, and subtree cascades can only be updated by their owner re-rendering.
- *Plain DI + C# `event`:* the baseline that leaves unsubscribe, `InvokeAsync`, `StateHasChanged`, error routing and current-value handling to every component.

**Consequences.** Services can take part (publish/subscribe) just like components. The library has to provide the lifecycle automation that DI doesn't (ADR-003).

---

## ADR-002: Events and state are separate primitives

**Status:** Accepted

**Context.** Event-only hubs force users to rebuild "current value" semantics on top, usually badly: late subscribers see nothing until the next event. State-only stores can't express "this happened" without inventing dummy state.

**Decision.** Two first-class primitives on the same hub: **`IEvent`** (fire-and-forget, routed) and **`IState<TSelf>`** (current value, provider-resolved, change-notified).

**Alternatives.**
- *Events only* (BlazorComponentBus-style).
- *State only* (Svelte-store-style).
- *Redux-style actions + reducers* (Fluxor): actions are events and reducers derive state, but every feature needs an action, a reducer and a feature registration.

**Consequences.** Two mental models to document. Clear guidance on when to use which ([state.md](state.md)). No reducers: state changes are direct `Set`/`Update` calls.

---

## ADR-003: Base class + source generator hybrid

**Status:** Accepted

**Context.** Automatic subscribe/unsubscribe/re-render needs lifecycle hooks. A source generator can only *add* code. It can't hook `OnInitialized` or `Dispose` without clashing with user overrides. A base class alone means reflection (trim/AOT-hostile on WebAssembly) or verbose manual registration.

**Decision.** **`RfEventComponent`** owns the lifecycle. A **source generator** emits only a `RegisterSubscriptions` override from `[Subscribe]`/`[UseState]`. **Analyzers** catch misuse. The manual `RegisterSubscriptions` override is a supported escape hatch.

**Alternatives.**
- *Generator only:* would have to override lifecycle methods, which conflicts with user code, or rely on re-implementing interfaces (fragile).
- *Base class + reflection:* simpler to build, but hurts trimming/AOT and moves errors to runtime.
- *Extension methods only:* no base class, but every component writes subscription and disposal plumbing.

**Consequences.** Components that need the full experience must derive from `RfEventComponent`, which can conflict with other libraries' base classes. Mitigated by the no-base-class paths in [components.md](components.md#using-the-hub-without-the-base-class) and ADR-006's compatibility mode.

---

## ADR-004: Annotated members require code-behind

**Status:** Accepted

**Context.** The Razor compiler is a source generator, and generators can't see each other's output. Members in `@code` blocks are invisible to our generator.

**Decision.** `[Subscribe]`/`[UseState]` members must be in `.razor.cs` partial classes. Analyzer `RFEVT001` reports an error when they're found in Razor-generated code.

**Alternatives.**
- *Reflection fallback for `@code` members:* reintroduces the trim/AOT problem for exactly those components.
- *Parsing `.razor` files as additional files:* fragile and duplicates the Razor compiler's work.

**Consequences.** Slightly more friction (one more file per event-aware component). Markup can still use `@code` for everything that isn't annotated. Confirmed with the .NET 10 Razor SDK: the generator never sees Razor output, and RFEVT001 catches attributes in `@code` blocks. See also ADR-017.

---

## ADR-005: `Bubble` is the default route

**Status:** Accepted

**Context.** With scopes, a publish with no explicit route still has to do something sensible.

**Decision.** `default(Route)` is `Route.Bubble`: origin scope, then each ancestor up to root.

**Alternatives.**
- *`Root`:* simpler to reason about when scopes are rare, but a component inside a scope couldn't reach its own scope's subscribers without an explicit route.
- *`Local`:* the most isolated, but surprising, because subscribers at root would stop receiving events from scoped publishers.

**Consequences.** Matches DOM intuition. Root subscribers see all bubbled events from anywhere, so a root-only app behaves like a plain global hub. Scoped events don't leak sideways or downward unless asked (`Broadcast`, `Global`).

---

## ADR-006: StateScope: Option C

**Status:** Accepted

**Context.** Subtree-scoped state could be built on Blazor's cascades or on the hub.

**Decision.** **Option C:** `<StateScope>` is backed by the hub. It provides a store, uses nearest-provider resolution, re-renders selectively, and supports selectors. An opt-in `CascadeValue="true"` *also* cascades the value for plain components. For root state, `options.CascadeRootState<T>()` does the same via `CascadingValueSource<T>`.

**Alternatives.**
- *Option A, built on cascades:* no base class needed, but every consumer re-renders on every change, writes need a separate setter, and root and subtree follow different code paths.
- *Option B, hub only:* selective and uniform, but unusable from plain or third-party components.

**Consequences.** Base-class consumers get selective re-rendering, and others can still read via `[CascadingParameter]`, paying normal cascade costs only when the flag is set.

---

## ADR-007: No JS and no cross-island bridge

**Status:** Accepted

**Context.** Interactive Server and WebAssembly islands on one page have separate DI containers. Bridging them requires JS interop (a browser relay) or a server round trip with circuit correlation.

**Decision.** **No JavaScript** in the library and **no cross-container communication**. Each container's hub is independent. The limitation is documented prominently ([render-modes.md](render-modes.md#no-cross-island-communication)).

**Alternatives.**
- *JS relay bridge with opt-in `[Bridged]` messages:* the only way to connect islands without a server round trip, but it brings serialization constraints, origin tagging, ordering issues and interop-availability timing.
- *Server hub bridge:* requires correlating a WebAssembly runtime with "its" circuit, plus extra network hops.

**Consequences.** A smaller, purely .NET library. Pages that mix interactive render modes can't coordinate those islands through the hub. Cross-tab sync is also out of scope.

---

## ADR-008: .NET 10 minimum

**Status:** Accepted

**Context.** The design uses C# 13 partial properties (generated `[UseState]` properties), C# 11 static abstract interface members (`IState<T>.Initial`), .NET 8 `DispatchExceptionAsync` and root-level cascading values, .NET 9 `RendererInfo`, and .NET 10 persistent-state improvements.

**Decision.** Target **`net10.0`** / C# 14. The generator assembly targets `netstandard2.0`, as Roslyn requires.

**Alternatives.** *.NET 8 LTS:* a wider audience, but it would lose partial properties (more awkward generated API), `RendererInfo` (harder non-interactive detection) and the newer persistence APIs.

**Consequences.** Smaller reach at first. No compatibility shims to maintain.

---

## ADR-009: Record equality for change detection

**Status:** Accepted

**Context.** Notifying on every `Set` causes render storms when values haven't changed.

**Decision.** Stores compare with `EqualityComparer<TState>.Default` and skip notifications when the value is equal. State types should be `record`s (`RFEVT013` warns otherwise). Selectors compare projections the same way.

**Alternatives.**
- *Always notify:* simple, wasteful.
- *Version counters only:* can't detect "set to the same value."
- *Deep structural comparison:* expensive and surprising for large graphs.

**Consequences.** Records with collection members compare collections **by reference**. Replacing a list with an equal but new list notifies, and mutating a list in place doesn't. Document this and recommend immutable collections.

---

## ADR-010: The root hub is Scoped

**Status:** Accepted

**Context.** On Blazor Server, Singleton services are shared across all users.

**Decision.** `IRfEventHub` is registered **Scoped**: per circuit (Server), per app instance (WebAssembly), per request (static SSR and prerender).

**Alternatives.** *Singleton:* would broadcast between users on Server. Never appropriate for UI events.

**Consequences.** Services depending on `IRfEventHub` must be Scoped or Transient. Prerender and interactive sessions get different hubs, which is why persistence exists (see [state.md](state.md#persistence-across-prerendering)).

---

## ADR-011: Scope objects are cascaded with `IsFixed`

**Status:** Accepted

**Context.** Non-fixed cascades make Blazor track and re-render every consumer when the value changes.

**Decision.** A scope object's identity never changes, so `EventScope`/`StateScope` cascade it with `IsFixed="true"`. All reactivity goes through hub subscriptions.

**Consequences.** Adding a scope costs nothing per descendant. A component's scope is fixed for its lifetime, so scope resolution happens once.

---

## ADR-012: Routes are origin + direction

**Status:** Proposed

**Context.** The agreed routing targets (Bubble, Local, Broadcast, Root, Named) mix two concerns: *where to start* and *which way to go*.

**Decision.** `Route` is a struct of `ScopeTarget Origin` × `RouteDirection Direction`, with presets for the agreed targets plus **`Route.Global`** (root + broadcast, meaning "everyone in this container"). `ScopeTarget` is reused by subscribers to choose which scope they attach to.

**Alternatives.** *A flat five-value enum:* simpler to read, but `Named` needs a parameter and combinations like "broadcast from a named scope" can't be expressed.

**Consequences.** One small extra concept. Covers every combination without growing the enum.

---

## ADR-013: `StateScope` creates its own scope

**Status:** Proposed

**Context.** A state provider has to attach to *some* scope. Attaching it to the nearest existing scope would mutate shared scopes, including root.

**Decision.** Each `<StateScope>` creates a new child scope that provides its `TState`.

**Alternatives.** *Attach to the nearest scope:* no extra routing node, but it mutates shared scopes and makes provider lifetime unclear.

**Consequences.** Inside a `StateScope`, the nearest scope is the `StateScope`, which affects `Route.Local` and `ScopeTarget.Nearest`. Providing several state types means nesting (multi-provide is an open question).

---

## ADR-014: Registration in `SetParametersAsync`, skipped when non-interactive

**Status:** Superseded by [ADR-016](#adr-016-subscribe-in-every-render-mode-supersedes-adr-014). The registration point (first `SetParametersAsync`) still stands; skipping subscriptions when non-interactive does not.

**Context.** Registration must happen after cascades are assigned, before user `OnInitialized`, and regardless of whether users call `base`. Prerendering instantiates components twice.

**Decision.** `RfEventComponent` registers in its first `SetParametersAsync`, after assigning parameters and before running the base lifecycle. When `RendererInfo.IsInteractive` is `false`, state handles are created (readable) but no subscriptions or notifications are registered.

**Consequences.** It's robust to users forgetting `base` calls. No double subscription during prerender. Derived components overriding `SetParametersAsync` must call `base`.

---

## ADR-015: Sequential, deterministic delivery

**Status:** Proposed

**Context.** Handlers could run sequentially or concurrently.

**Decision.** Sequential, awaited, in a deterministic order (route order of scopes, then subscription order). `PublishAsync` completes when all handlers have completed. Failures are isolated per handler.

**Alternatives.** *Parallel (`Task.WhenAll`):* faster for I/O-heavy handlers, but non-deterministic ordering and harder reasoning about shared component state. Tracked as an open question.

**Consequences.** Predictable behavior that's easy to debug. A slow handler delays later handlers. Handlers that start long-running work should not await it inline.

---

## ADR-016: Subscribe in every render mode (supersedes ADR-014)

**Status:** Accepted (found during implementation)

**Context.** ADR-014 skipped subscriptions when `RendererInfo.IsInteractive` was `false`, reasoning that static and prerendered components never re-render. Running the checkout sample showed that this is wrong. The static renderer keeps rendering until async initialization settles. `SavedCartLoader` wrote the cart after an `await`, but `CartSummary` and `CartBadge` had already rendered without subscribing, so the prerendered HTML showed an empty cart. The persisted state was correct, so the interactive circuit fixed it a moment later, which is exactly the flicker persistence is meant to avoid.

**Decision.** `RfEventComponent` registers its subscriptions in every render mode. `StateScope` likewise always subscribes for `CascadeValue`.

**Alternatives.** *Subscribe to state only, not events, during non-interactive rendering:* events published during a prerender are just as able to affect the HTML, and the split adds a rule to remember for no benefit.

**Consequences.** Prerendered and static HTML reflects state written anywhere during the render. There's no leak: the request-scoped hub, its components and their subscriptions are disposed together when the request ends. There's no double delivery, because prerender and interactive instances use different root hubs. Covered by the test `Prerendered_component_sees_state_set_later_in_the_same_render` and by the checkout sample.

---

## ADR-017: Base class goes in `@inherits`

**Status:** Accepted (found during implementation)

**Context.** The spec originally said to declare `: RfEventComponent` in the code-behind instead of using `@inherits`, so the generator could see it. In practice the Razor compiler emits `: ComponentBase` on its partial declaration whenever a `.razor` file has no `@inherits`. A code-behind declaring a different base then fails with CS0263.

**Decision.** `.razor` files use `@inherits RfEventComponent`. The code-behind may repeat the base class or omit it. The generator doesn't depend on seeing the base class: it always emits the override, and `RFEVT003` checks the base against the final compilation (which includes Razor's output).

**Consequences.** One extra line per `.razor` file. A folder-level `_Imports.razor` with `@inherits RfEventComponent` also works, but it applies to every component in the folder, so per-file is clearer.
