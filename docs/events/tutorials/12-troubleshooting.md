# 12 · Troubleshooting

## Build diagnostics

The analyzer that ships with the package reports these. Errors stop the build. Warnings and info point at likely bugs.

| ID | Severity | Message (short) | Fix |
|---|---|---|---|
| `RFEVT001` | Error | Attribute used in a `.razor` `@code` block | Move the member to the `.razor.cs` code-behind. The generator can't see code inside `.razor` files. |
| `RFEVT002` | Error | Type (or a containing type) isn't `partial` | Add `partial` to the class and to any class it's nested in |
| `RFEVT003` | Error | Type doesn't derive from `RfEventComponent` | Add `@inherits RfEventComponent` to the `.razor` file (or `: RfEventComponent` for a C#-only component) |
| `RFEVT004` | Error | Invalid `[Subscribe]` signature | Use `void M(E)`, `Task/ValueTask M(E)`, or `Task/ValueTask M(E, CancellationToken)`. Non-static, non-generic, no `ref`/`out`. |
| `RFEVT005` | Error | Handler's first parameter isn't an `IEvent` | Make the type implement `IEvent`, or fix the parameter type |
| `RFEVT006` | Error | `[UseState]` property isn't a partial get-only declaration | Write it as `private partial T Name { get; }`, non-static and without a body |
| `RFEVT007` | Error | `[UseState]` property type isn't a state type | Use a type implementing `IState<T>`, or add `Select = …` |
| `RFEVT008` | Error | Invalid selector | `static TProperty Select(TState state)` in the same class. Tuple names may differ. |
| `RFEVT009` | Error | Attributes **and** a `RegisterSubscriptions` override | Move the manual code into `partial void OnRegisterSubscriptions(SubscriptionBuilder builder)` |
| `RFEVT010` | Warning | Inconsistent `Scope`/`ScopeName` | Use either `Scope = ScopeKind.Root` or `ScopeName = "x"`, not both |
| `RFEVT011` | Warning | Two handlers for the same event in one component | Merge them. Both would run. |
| `RFEVT012` | Info | Event type isn't immutable | Use a `record` with init-only properties |
| `RFEVT013` | Warning | State type isn't a `record` | Use a `record`, so equal values don't re-render everything |
| `RFEVT014` | Warning | Component implements `IDisposable`/`IAsyncDisposable` itself | Override `DisposeAsyncCore` instead |

Compiler errors you may see alongside them:

| Error | Meaning |
|---|---|
| `CS0263` *(different base classes)* | Base class declared in the code-behind but not in the `.razor` file. Add `@inherits RfEventComponent`. |
| `CS9248` *(partial property must have an implementation part)* | The generator didn't run. The project containing the component needs its own `RForge.Blazor.Events` package reference, because analyzers don't flow through project references ([setup step 2](01-setup.md#2-reference-the-package)). |

## Runtime problems

### "My component doesn't update"

Check in this order:
1. Does it **read** the state through `[UseState]` (or a handle)? `State<T>().Value` in markup is a one-off read with no subscription.
2. Is the state **replaced** rather than mutated? `Update(s => s with { … })` notifies. `s.Items.Add(…)` on a mutable list doesn't.
3. Does a selector return the **same value**? Then not re-rendering is correct.
4. Is the writer in a **different render mode** (Server vs. WebAssembly island)? Different modes use different hubs ([tutorial 09](09-render-modes.md)).
5. For events: was the handler mounted **before** the publish? Events aren't replayed.

### "My handler never runs"

- The listener is inside an `EventScope`/`StateScope` and the event was published from **outside** it. Publishes bubble *up*, not down into other scopes. Publish with `Route.Global`, or listen with `[Subscribe(Scope = ScopeKind.Root)]` ([tutorial 05](05-scopes-and-routing.md)).
- The publish used `Route.Local` or `Route.Broadcast` from a scope the listener isn't in.
- The listener's component is rendered in a different render mode from the publisher.

### Exceptions

| Exception | Cause | Fix |
|---|---|---|
| `RfEventScopeNotFoundException` | `Route.Named("x")` or `ScopeName = "x"` with no enclosing scope named `x` | Check the name, and that the component is inside that scope |
| `RfEventReentrancyException` | Handlers publish events that trigger each other (A → B → A …) | Break the loop, or skip publishing when nothing changed |
| `ObjectDisposedException` from the hub | Using a scope or store after its component/circuit ended (often a timer) | Stop background work on dispose. Catch it in timer callbacks. |
| `InvalidOperationException: SubscriptionBuilder can only be used inside RegisterSubscriptions` | The builder was stored and used later | Use `Scope.Subscribe` ([tutorial 10](10-manual-registration.md#4-subscribe-later-or-to-something-decided-at-runtime)) |
| `No registered service of type 'RForge.Blazor.Events.IRfEventHub'` | `AddRForgeEvents` missing in this host (often the WebAssembly client) | Register in both `Program.cs` files |
| `MissingRendererInfoException` (tests) | bUnit needs `SetRendererInfo` | See [tutorial 11](11-testing-with-bunit.md) |

### Other surprises

| Surprise | Explanation |
|---|---|
| One user sees another user's data on Server | A service holding `IRfEventHub` is registered as **Singleton**. Make it Scoped. |
| Changing `StateScope`'s `Initial` parameter does nothing | It's read once (a warning is logged). Write to the store. |
| A `StateScope` breaks `Route.Local` | A `StateScope` is also a scope: inside it, "local" means the `StateScope` |
| State resets when an Auto page later runs in WebAssembly | New runtime, new hub. Reload from the source. |
| State loaded during prerender disappears | Not opted into persistence ([tutorial 08](08-prerendering-and-persistence.md)) |
| A click feels slow | A handler awaits long work. Delivery is sequential, so fire and forget. |
| Analyzer errors don't appear, only compiler errors | The C# compiler skips analyzers when other compile errors exist. Fix those first and the `RFEVT` errors will show. |

## Still stuck?

- Look at the generated code (`*.Hub.g.cs`, [setup step 7](01-setup.md#7-check-that-the-generator-ran)) to see exactly what was registered.
- Compare with the matching page in the [sample app](../../../samples).
- Read the relevant part of the [design spec](../design/index.md): [events](../design/events.md), [state](../design/state.md), [scoping](../design/scoping.md), [render modes](../design/render-modes.md).
