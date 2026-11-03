# 13 · Best practices

A one-page checklist of the recommendations from the tutorials. Each item links to the tutorial that explains it.

## Project layout

```
MyApp/                                  server project
├── Program.cs                          AddRForgeEvents(EventHubSetup.Configure)
├── Services/                           server-only Scoped services that publish/subscribe
└── Components/Pages/                   pages that need server-only services (InteractiveServer)
MyApp.Client/                           anything that may run in WebAssembly
├── Program.cs                          AddRForgeEvents(EventHubSetup.Configure)
├── EventHubSetup.cs                         the single hub configuration
├── AppJsonContext.cs                   JSON metadata for persisted state
├── GlobalUsings.cs                     global using RForge.Blazor.Events;
├── _Imports.razor                      @using RForge.Blazor.Events
├── Features/<Feature>/
│   ├── <Feature>State.cs
│   ├── <Feature>Events.cs
│   └── Components/X.razor + X.razor.cs
└── Pages/
MyApp.Tests/                            bUnit tests, one class per feature
```

## Where each file goes

| File | Put it in | Why | Tutorial |
|---|---|---|---|
| `IState` records | `MyApp.Client/Features/<Feature>/<Feature>State.cs` | WebAssembly needs them, and the server references the client | [02](02-state.md) |
| `IEvent` records | `MyApp.Client/Features/<Feature>/<Feature>Events.cs` | Same, and they sit next to the state they relate to | [04](04-events.md) |
| Event-aware components | `MyApp.Client/Features/<Feature>/Components/` | Run in every render mode, and stay next to their state | [01](01-setup.md) |
| Component attributes (`[Subscribe]`, `[UseState]`) | The `.razor.cs` code-behind | The generator can't see `@code` blocks | [01](01-setup.md) |
| `@inherits RfEventComponent` | The `.razor` file | Otherwise Razor declares `: ComponentBase` (CS0263) | [01](01-setup.md) |
| `EventHubSetup.Configure` | `MyApp.Client/EventHubSetup.cs` | Server and WebAssembly hubs must be configured identically | [01](01-setup.md) |
| `JsonSerializerContext` | `MyApp.Client/AppJsonContext.cs` | One place listing every persisted state | [08](08-prerendering-and-persistence.md) |
| Services using databases/secrets | `MyApp/Services/` | Must not ship to the browser | [07](07-services-and-background-work.md) |
| `EventScope`/`StateScope` | The feature's root component | Works wherever the feature is placed, including inside islands | [05](05-scopes-and-routing.md), [06](06-scoped-state.md) |
| Event tests | `MyApp.Tests/<Feature>Tests.cs` | One test class per feature mirrors the source layout | [11](11-testing-with-bunit.md) |

## Naming

| Thing | Convention | Example |
|---|---|---|
| State | `<Noun>State` | `CartState`, `ThemeState` |
| Event: a fact | Past tense | `ItemAdded`, `SectionSaved` |
| Event: a command | `<Verb>Requested` | `ToastRequested`, `SaveAllRequested` |
| Selector | `Select<What>` | `SelectCount`, `SelectIsEven` |
| Handler | `On<Event>` | `OnItemAdded` |
| Scope name | Feature name, lowercase | `"checkout"`, `"settings"` |

## State

- [ ] `sealed record` implementing `IState<T>` with a sensible `Initial`. ([02](02-state.md))
- [ ] Immutable: replace with `with`. `ImmutableList`/`ImmutableDictionary` for collections. ([02](02-state.md#7-state-that-holds-a-collection))
- [ ] One state per concept; no giant "AppState". ([02](02-state.md))
- [ ] Domain rules live on the record (`cart.Add(product)`), not in components. ([02](02-state.md))
- [ ] Readers use `[UseState]` (with a selector for one or two fields). Writers use `State<T>()`. ([02](02-state.md#4-re-render-less-with-a-selector))
- [ ] Persist only expensive-to-load root state, always "load if not loaded", and `[JsonIgnore]` derived properties. ([08](08-prerendering-and-persistence.md))

## Events

- [ ] `sealed record` implementing `IEvent`, carrying ids and small values only. ([04](04-events.md))
- [ ] Use an event for "it happened" and state for "what it is now". Often you'll do both. ([04](04-events.md#events-or-state))
- [ ] Handlers are fast: fire-and-forget long work with `DisposalToken`. ([04](04-events.md#7-keep-handlers-fast))
- [ ] App-wide listeners (toasts, logging) use `ScopeKind.Root`. ([04](04-events.md), [05](05-scopes-and-routing.md))
- [ ] Don't use the hub for direct parent → child. Use `[Parameter]`/`EventCallback`. ([04](04-events.md))

## Scopes

- [ ] Commands go **down** (`Route.Named("x", Broadcast)`), facts go **up** (default `Bubble`). ([05](05-scopes-and-routing.md))
- [ ] Target scopes by name rather than by position. ([05](05-scopes-and-routing.md#3-broadcast-a-command-into-the-scope))
- [ ] Multiple widget instances each get a `StateScope`, with starting data passed through `Initial`. ([06](06-scoped-state.md))
- [ ] Child components are written as if the state were global. ([06](06-scoped-state.md#3-write-the-children-as-if-the-state-were-global))

## Services and threads

- [ ] Services that use `IRfEventHub` are **Scoped**, never Singleton. ([07](07-services-and-background-work.md))
- [ ] Every `hub.Subscribe(…)` / `store.Subscribe(…)` in a service is disposed. ([07](07-services-and-background-work.md#5-subscribe-from-a-service))
- [ ] Background work stops when its page or circuit goes away, and timers catch `ObjectDisposedException`. ([07](07-services-and-background-work.md))
- [ ] Components never call `InvokeAsync` for hub updates. The base class does. ([07](07-services-and-background-work.md#3-components-just-read))

## Render modes

- [ ] `AddRForgeEvents(EventHubSetup.Configure)` in **both** `Program.cs` files. ([01](01-setup.md#4-register-the-hub-in-both-hosts))
- [ ] One interactive render mode per area that must communicate. ([09](09-render-modes.md#2-mixed-islands-dont-talk-to-each-other))
- [ ] Scopes are declared inside the interactive component. ([09](09-render-modes.md#3-declare-scopes-inside-the-island))
- [ ] `prerender: false` for pages that start background work. ([07](07-services-and-background-work.md#4-start-and-stop-from-a-server-page))

## Advanced

- [ ] Prefer attributes. Use manual registration only for what they can't express, and comment why. ([10](10-manual-registration.md))
- [ ] With attributes, extend via `partial void OnRegisterSubscriptions`, never a second override. ([10](10-manual-registration.md#3-mix-attributes-and-manual-code))
- [ ] Components whose captured parameters can change get a `@key`. ([10](10-manual-registration.md#1-a-selector-that-depends-on-a-parameter))

## Testing

- [ ] A shared `EventTestContext` that adds the hub and sets `RendererInfo`. ([11](11-testing-with-bunit.md#2-a-base-class-for-hub-tests))
- [ ] Arrange through `Hub.State<T>()`, act inside `cut.InvokeAsync`, and use spies for published events. ([11](11-testing-with-bunit.md))
