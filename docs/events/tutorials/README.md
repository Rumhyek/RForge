# RForge.Blazor.Bus tutorials

Step-by-step guides for using RForge.Blazor.Bus in a Blazor app. They assume you already know Blazor components, dependency injection and render modes, and explain only what's specific to the bus.

Start with **Setup**. After that, each tutorial stands alone: it has its own small example, so you can jump straight to the topic you need.

| # | Tutorial | You'll learn |
|---|---|---|
| 01 | [Setup](01-setup.md) | Install the package, register the bus, lay out your project, build your first bus-aware component |
| 02 | [State](02-state.md) | Define state, read it reactively with `[UseState]`, write it, and use selectors to limit re-renders |
| 03 | [State with cascades](03-state-with-cascades.md) | Let plain (or third-party) components read bus state through `[CascadingParameter]` |
| 04 | [Events](04-events.md) | Define events, publish them, handle them with `[Subscribe]`, and decide when to use an event vs. state |
| 05 | [Scopes and routing](05-scopes-and-routing.md) | Fence events into a subtree with `<EventScope>` and choose where events travel with routes |
| 06 | [Scoped state](06-scoped-state.md) | Give each instance of a widget its own copy of a state with `<StateScope>` |
| 07 | [Services and background work](07-services-and-background-work.md) | Publish and subscribe from services, timers and other threads |
| 08 | [Prerendering and persistence](08-prerendering-and-persistence.md) | Keep state loaded during prerendering when the page becomes interactive |
| 09 | [Render modes](09-render-modes.md) | What works where (Static, Server, WebAssembly, Auto), and the island boundary |
| 10 | [Manual registration](10-manual-registration.md) | Subscribe without attributes: parameter-dependent selectors, dynamic subscriptions |
| 11 | [Testing with bUnit](11-testing-with-bunit.md) | Unit-test bus-aware components |
| 12 | [Troubleshooting](12-troubleshooting.md) | Every `RFBUS` diagnostic and the most common runtime mistakes |
| 13 | [Best practices](13-best-practices.md) | One-page checklist: project layout, file placement, naming, design rules |

## Two conventions used everywhere

Every bus-aware component in these tutorials follows two rules. [Setup](01-setup.md) explains why.

1. **The `.razor` file starts with `@inherits BusComponentBase`.**
2. **`[Subscribe]` and `[UseState]` members live in the code-behind (`.razor.cs`)**, never in an `@code` block.

Each code block starts with a comment giving the **file path** it belongs in, for example `// MyApp.Client/Features/Counter/CounterState.cs`. The paths follow the [recommended project layout](01-setup.md#1-lay-out-the-project).

## See also

- [Sample app](../../../samples): seven runnable example pages. Each tutorial links to the one that matches.
- [Design specification](../design/index.md): why the library works the way it does.
