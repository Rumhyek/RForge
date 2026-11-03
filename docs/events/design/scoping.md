# Scoping

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

Without scopes, the hub is one global channel per DI container, and that's enough for many apps. Scopes cover the cases where "global" is too broad:

- **Isolation:** two instances of the same widget on a page must not hear each other's events.
- **Independent state:** each instance needs its own copy of a state type.
- **Targeting:** "notify everything inside this dialog" or "tell the checkout section, wherever I am inside it."

This is the same split Svelte makes: **context** decides *which* instance a subtree sees, and the **store** inside it supplies reactivity. Here, a cascaded scope object plays the role of context, and the hub/stores provide the reactivity.

## The scope tree

Scopes form a tree that mirrors (a subset of) the component tree:

- The **root** is the `IRfEventHub` from DI. It always exists and is never disposed by components.
- `<EventScope>` and `<StateScope>` each create a **child scope** of their nearest enclosing scope, and cascade it to their subtree.
- `RfEventComponent` receives the nearest scope as a cascading parameter and falls back to root when there's none.

```razor
@* MainLayout.razor (inside an interactive island) *@
<EventScope Name="app">
    <NavMenu />
    <EventScope Name="checkout">
        <StateScope TState="CheckoutForm">
            <AddressForm />
            <PaymentForm />
        </StateScope>
        <OrderSummary />
    </EventScope>
</EventScope>
```

```
Root (IRfEventHub)
└── "app"
    └── "checkout"
        └── (unnamed, provides CheckoutForm)
```

## `<EventScope>`

| Parameter | Type | Meaning |
|---|---|---|
| `Name` | `string?` | Optional. Makes the scope addressable by `ScopeTarget.Named` / `Route.Named`. Names don't have to be unique; lookup takes the **nearest ancestor-or-self** match. |
| `ChildContent` | `RenderFragment` | The subtree that sees this scope. |

Lifecycle:
1. On initialization it creates a child `IRfEventHubScope` whose `Parent` is the cascaded scope (or root).
2. It renders `<CascadingValue TValue="IRfEventHubScope" Value="_scope" IsFixed="true">`.
3. On disposal it disposes the scope (see [Disposal](#disposal)).

`EventScope` doesn't derive from `RfEventComponent` and never re-renders because of hub activity.

## `<StateScope>`

| Parameter | Type | Meaning |
|---|---|---|
| `TState` | type param | The state type this scope provides. Must implement `IState<TState>`. |
| `Name` | `string?` | Optional, as for `EventScope`. |
| `Initial` | `TState?` | Starting value. Defaults to `TState.Initial`. Only read at initialization. Later changes to the parameter are ignored, and a warning is logged. |
| `CascadeValue` | `bool` | Default `false`. When `true`, also cascades the current `TState` value (non-fixed) for plain components. See [state.md](state.md#compatibility-mode-cascadevalue). |
| `ChildContent` | `RenderFragment` | The subtree. |

### StateScope is a scope

A `StateScope` is a full scope, not just a provider attached to an existing scope. That's a deliberate simplification. Attaching providers to the nearest existing scope would mean mutating root, which is shared by the whole container. As a result:

- Events published inside a `StateScope` treat it as their **nearest** scope. `Route.Local` from inside a `StateScope` reaches only that `StateScope`'s subscribers.
- Bubbling passes through it transparently, so it has no effect on `Bubble`/`Root`/`Global`.
- Give it a `Name` if you need to target it.

To provide several state types to one subtree, nest them:

```razor
<StateScope TState="CheckoutForm">
    <StateScope TState="PaymentDraft">
        ...
    </StateScope>
</StateScope>
```

Whether a single scope should be able to provide several state types (`<StateScope TState="..."><Provide TState="..." />`) is an [open question](index.md#open-questions).

## Why the cascade is fixed

A non-fixed `CascadingValue` makes Blazor track every consumer and re-render them when the value changes. That's exactly the cost that makes broad cascades expensive.

The **scope object's identity never changes** for the lifetime of the `EventScope`/`StateScope` that created it. All change notification flows through hub subscriptions, not through the cascade. So the scope can be cascaded with `IsFixed="true"`:

- Blazor doesn't track consumers of a fixed cascade, so adding a scope costs nothing per descendant.
- Re-rendering an `EventScope` (for example, because its parent re-rendered) never forces descendants to re-render.
- The only re-renders come from components' own subscriptions, and those are selective.

The one exception is `StateScope` with `CascadeValue="true"`. It *additionally* renders a **non-fixed** `CascadingValue<TState>`, and its consumers pay normal cascade costs. That's the explicit trade-off of [compatibility mode](state.md#compatibility-mode-cascadevalue).

See [ADR-011](decisions.md#adr-011-scope-objects-are-cascaded-with-isfixed).

## Resolution rules (summary)

| Operation | Rule |
|---|---|
| Component's `Scope` | Nearest cascaded `IRfEventHubScope`, else root |
| `ScopeTarget.Nearest` | `Scope` |
| `ScopeTarget.Root` | Root |
| `ScopeTarget.Named(n)` | Walk from `Scope` upward (including `Scope` itself). First scope with `Name == n` (ordinal comparison). None → `RfEventScopeNotFoundException` |
| State provider | From the target scope, walk upward to the first scope that provides `TState`. Root implicitly provides every state type. |
| Event routing | See [events.md](events.md#routing) |

Resolution happens **once**, when the component registers. Because scopes are fixed cascades, a component's scope can't change without the component being recreated.

## Disposal

When an `EventScope`/`StateScope` is disposed (its subtree is removed from the render tree):

1. The scope is marked disposed, and further `PublishAsync`, `Subscribe` or `State` calls on it throw `ObjectDisposedException`.
2. Any remaining subscriptions on that scope are released. Normally descendant components have already disposed their own subscriptions, but this guards against non-component subscribers that forgot to.
3. A `StateScope`'s store is disposed, and handles that still reference it stop receiving notifications.
4. The scope detaches from its parent, so it's no longer visited by `Broadcast`/`Global`.

Blazor disposes children before parents, so descendant components clean up before their scope does. In-flight deliveries skip disposed subscribers (see [events.md](events.md#subscriptions-added-or-removed-during-delivery)).

## Render-mode boundaries

> **Cascading values do not cross render-mode boundaries.** A scope declared in a statically rendered layout is **not** visible to an interactive island inside it. Blazor can't serialize the scope object across that boundary.

Practical rules:
- Declare `EventScope` and `StateScope` **inside** the interactive island (inside the component that has `@rendermode`, or in a layout that is itself interactive).
- If the whole app is interactive (render mode set on `Routes`), you can put scopes anywhere, including layouts.
- The root hub *is* available everywhere, because each DI container registers its own. But Server and WebAssembly containers are **separate hubs**. See [render-modes.md](render-modes.md#no-cross-island-communication).

## When not to use scopes

- **Truly global concerns** (current user, theme, cart): use root state and root/global events. No scope is needed.
- **Parent ↔ direct child:** use `[Parameter]` and `EventCallback`. The hub is for communication that doesn't follow the parent chain. Using it where a parameter would do makes the data flow harder to follow.
