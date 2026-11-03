# Render Modes

> **Status:** Draft · 2026-10-06 · Part of the [RForge.Blazor.Events design spec](index.md)

The library supports every Blazor render mode: static SSR, Interactive Server, Interactive WebAssembly and Interactive Auto, with or without prerendering. One rule shapes everything below:

> **A hub lives in a DI container. Communication happens within one container, never across containers.**

## DI lifetime

`AddRForgeEvents` registers the root `IRfEventHub` as **Scoped**. What "scoped" means depends on the host:

| Host | One root hub per… | Shared by |
|---|---|---|
| Static SSR | HTTP request | Components rendered in that request |
| Interactive Server | Circuit | **All** Interactive Server components on the page, for one user, for the life of the circuit |
| Interactive WebAssembly | Browser app instance | **All** WebAssembly components on the page (they share one runtime) |
| Prerender (of either interactive mode) | HTTP request | The prerender pass only. Discarded afterward. |

The hub is **never** a Singleton. On Blazor Server, a singleton hub would deliver one user's events to every connected user. See [ADR-010](decisions.md#adr-010-the-root-hub-is-scoped).

`AddRForgeEvents()` must be called in **each** `Program.cs` that hosts interactive components: the server project always, and the `.Client` project for WebAssembly or Auto.

## Per-mode behavior

| Mode | Events | State | Notes |
|---|---|---|---|
| **Static SSR** | Full, within the request | Full, until the response is sent | One hub per request. Nothing updates after the HTML is sent. |
| **Interactive Server** | Full | Full | Handlers marshaled through `InvokeAsync` (publishers may be off-thread) |
| **Interactive WebAssembly** | Full | Full | Single-threaded. `InvokeAsync` is effectively inline. Generated wiring is trim/AOT-safe. |
| **Interactive Auto** | Full, in whichever runtime the component landed in | Full, per runtime | See [Auto](#interactive-auto) |
| **Prerendering** | As static SSR | As static SSR, plus optional [persistence](state.md#persistence-across-prerendering) | Prerender and interactive instances use different hubs |

## Static SSR

Statically rendered components produce HTML once. Within that render, the hub works normally:
- Components subscribe as usual (see [components.md](components.md#non-interactive-rendering-static-ssr-and-prerender)). The static renderer keeps rendering until async initialization settles, so a component that writes state after an `await` still updates components rendered before it.
- `PublishAsync` delivers to component and non-component subscribers in the request scope.
- When the request ends, the request's hub, its subscriptions and its components are disposed together. With streaming rendering (`[StreamRendering]`), updates keep streaming until the render completes.

## Prerendering

An interactive component that prerenders is instantiated **twice**: once in the prerender request scope (non-interactive) and once in the interactive scope.

- **No double delivery.** The two instances subscribe to *different* root hubs (request scope vs. circuit/runtime), so an event published interactively reaches only the interactive instance.
- **Correct prerendered HTML.** State loaded asynchronously during prerendering reaches every subscriber before the HTML is sent (verified by the checkout sample).
- **State is lost by default.** The interactive session has a new root hub. Opt-in persistence (`options.PersistState(...)`) carries root-provided state across. See [state.md](state.md#persistence-across-prerendering).
- **Events are never persisted.** They're fire-and-forget by definition.

## Interactive Auto

An Auto component runs on Server for early visits (while the WebAssembly runtime downloads) and on WebAssembly afterward. A given component **instance** is one or the other, never both.

- A component in the Server phase talks to the circuit's hub. A component in the WebAssembly phase talks to the WebAssembly hub.
- **State doesn't migrate** from the circuit to the WebAssembly runtime. A later navigation that lands on WebAssembly starts from `Initial` (or persisted prerender state, if that page prerenders).
- For state that has to survive the move between runtimes, use an external source of truth (the server API, browser storage via your own service) and load it into hub state on initialization.

## No cross-island communication

> **Interactive Server islands and Interactive WebAssembly islands on the same page cannot communicate through the hub.** They have separate DI containers and therefore separate root hubs.

```
Browser page
├── Server islands A, B  ──▶  circuit hub       A ⇄ B   ✅
└── WASM islands C, D    ──▶  WebAssembly hub   C ⇄ D   ✅

A/B ⇄ C/D   ❌  separate DI containers, no bridge
```

This is a **deliberate non-goal** ([ADR-007](decisions.md#adr-007-no-js-and-no-cross-island-bridge)). Bridging would need either JS interop (a browser-side relay) or a server round trip with circuit correlation. Both add serialization constraints, ordering problems and failure modes far beyond the rest of the library.

Guidance:
- Use **one interactive render mode per page region that has to communicate.** Most apps already do: a global render mode on `Routes`, or one mode per page.
- If two islands of different modes must coordinate, use an app-level channel (an API call, a SignalR hub, browser storage) and translate it into hub events on each side.

## Scopes and render-mode boundaries

Cascading values don't flow across render-mode boundaries, so `<EventScope>`/`<StateScope>` must be declared **inside** the interactive island. See [scoping.md](scoping.md#render-mode-boundaries).

## Checklist for app authors

- [ ] `AddRForgeEvents()` in the server `Program.cs`
- [ ] `AddRForgeEvents()` in the `.Client` `Program.cs` (WebAssembly / Auto)
- [ ] Scopes declared inside interactive components (or the whole app is interactive)
- [ ] Components that must talk to each other share a render mode
- [ ] `PersistState(...)` for root state loaded during prerender
- [ ] `.razor` files of event-aware components start with `@inherits RfEventComponent`
- [ ] No Singleton services depending on `IRfEventHub`
