# RForge.Blazor.Events

**Typed events and reactive state for Blazor components, wherever they sit in the tree.**

> **Status:** new in RForge.Blazor 1.0.0. Tested with 96 unit/generator tests on each of net8, net9 and net10, plus a browser-driven pass over every sample page. See the [design specification](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/index.md).

Blazor makes parent ↔ child communication easy. Communicating *across* the tree usually means a hand-written service with C# events, plus subscribing, unsubscribing, `InvokeAsync`, `StateHasChanged` and error handling in every component that uses it.

RForge.Blazor.Events handles that for you. It's inspired by Svelte's stores and context and adapted to Blazor's DI and rendering model.

## Features

- **Events:** typed, fire-and-forget messages (`IEvent`) that any component or service can publish and handle.
- **State:** store-like values (`IState<T>`) with a current value, change notification, and no-op suppression through record equality.
- **Automatic lifecycle:** derive from `RfEventComponent` and subscriptions, re-renders, thread marshaling, `ErrorBoundary` integration and disposal are handled for you.
- **Selectors:** re-render only when the part of the state you use changes.
- **Scopes:** `<EventScope>` and `<StateScope>` isolate events and give subtrees their own state instances. Routes include `Bubble` (default), `Local`, `Broadcast`, `Root`, `Global` and `Named`.
- **Source generator:** `[Subscribe]` and `[UseState]` attributes, with no reflection, so it's trim/AOT safe. Analyzers (`RFEVT001`–`RFEVT014`) catch mistakes at build time.
- **Every render mode:** static SSR, Interactive Server, WebAssembly and Auto, with or without prerendering, plus opt-in state persistence from prerender to interactive.
- **Works with plain components too:** opt-in cascading values let third-party components read state through `[CascadingParameter]`.

## Install

```bash
dotnet add package RForge.Blazor.Events
```

One package contains the runtime, the source generator and the analyzers. Add it to **every project that contains event-aware components** (in a Blazor Web App, both the server and the `.Client` project), because NuGet doesn't pass analyzers through project references. See [Setup](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/01-setup.md) for the full walkthrough.

## A quick look

**Register** (in every `Program.cs` that hosts interactive components, including the `.Client` project for WebAssembly/Auto):

```csharp
builder.Services.AddRForgeEvents();
```

**Define events and state:**

```csharp
public sealed record ItemAdded(int ProductId, int Quantity) : IEvent;

public sealed record CartState(int Count, decimal Total) : IState<CartState>
{
    public static CartState Initial => new(0, 0m);
}
```

**Publish and write state** from any component:

```razor
@* ProductCard.razor *@
@inherits RfEventComponent
<button @onclick="AddToCart">Add</button>
```

```csharp
// ProductCard.razor.cs
public partial class ProductCard
{
    [Parameter] public Product Product { get; set; } = default!;

    private async Task AddToCart()
    {
        State<CartState>().Update(s => s with { Count = s.Count + 1, Total = s.Total + Product.Price });
        await PublishAsync(new ItemAdded(Product.Id, 1));
    }
}
```

**React** anywhere else in the tree, with no shared parent needed:

```razor
@* CartBadge.razor *@
@inherits RfEventComponent
<span class="badge @(_pulse ? "pulse" : "")">@Count</span>
```

```csharp
// CartBadge.razor.cs
public partial class CartBadge
{
    [UseState(Select = nameof(SelectCount))]
    private partial int Count { get; }                // re-renders only when Count changes
    private static int SelectCount(CartState s) => s.Count;

    [Subscribe]
    private void OnItemAdded(ItemAdded e) => _pulse = true;

    private bool _pulse;
}
```

**Scope** a subtree so each instance gets its own state and its own event channel:

```razor
<EventScope Name="checkout">
    <StateScope TState="CheckoutForm">
        <AddressForm />
        <PaymentForm />
    </StateScope>
    <OrderSummary />
</EventScope>
```

## Render-mode support

| Mode | Support |
|---|---|
| Static SSR | Full within the request: one hub per request. Nothing updates after the HTML is sent. |
| Interactive Server | Full. One hub per circuit (per user). |
| Interactive WebAssembly | Full. One hub per browser app instance. |
| Interactive Auto | Full, within whichever runtime the component is running in. |
| Prerendering | Prerendered HTML reflects state loaded during the render. Opt-in `PersistState(...)` carries root state into the interactive session. |
| Server ↔ WebAssembly islands on the same page | ❌ **Not supported.** Separate DI containers mean separate hubs. |

Details: [docs/events/design/render-modes.md](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/render-modes.md).

## Requirements

- **.NET 8, 9 or 10.** `[Subscribe]`, state, scopes and the analyzers work with the .NET 8 SDK and newer.
- **`[UseState]` needs C# 13**, because it is declared as a partial property. net9 and net10 projects already compile at C# 13 or later. A net8 project defaults to C# 12, so it needs the .NET 9 SDK or newer and `<LangVersion>13</LangVersion>` (or `latest`).
- **Async lambdas at C# 12.** In a net8 project at the default C# 12, `builder.On<MyEvent>(async e => ...)` is ambiguous between the `Task` and `ValueTask` overloads (CS0121). Pass a method group, cast the lambda to `Func<MyEvent, Task>`, or set `LangVersion` to 13 or later, which resolves it.
- Members annotated with `[Subscribe]` / `[UseState]` must be in a **code-behind** (`.razor.cs`) partial class, not in a `@code` block. Source generators can't see the Razor compiler's output, and `RFEVT001` reports it if you forget ([ADR-004](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/decisions.md#adr-004-annotated-members-require-code-behind)).
- Event-aware `.razor` files start with **`@inherits RfEventComponent`**. Razor otherwise declares `: ComponentBase`, which conflicts with a base class declared in the code-behind ([ADR-017](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/decisions.md#adr-017-base-class-goes-in-inherits)).

## Samples

`samples/` contains a Blazor Web App (Server + WebAssembly + Auto) with seven example pages:

| Page | Level | Render mode | Shows |
|---|---|---|---|
| Shared state | Simple | Auto | `IState`, `[UseState]`, selectors, record-equality no-ops |
| Notifications | Simple | Server | `IEvent`, `[Subscribe]`, deep publishers, events vs. state |
| Plain components | Simple | WebAssembly | `CascadeRootState`, `StateScope CascadeValue`, nearest provider |
| Scoped widgets | Complex | Auto | `StateScope` per widget, `Route.Local` / `Bubble` / `Global`, tuple selectors, scope disposal |
| Checkout | Complex | Server (prerendered) | `PersistState`, named routes, async handlers + cancellation, `ErrorBoundary` isolation |
| Live dashboard | Complex | Server | Off-thread publishing from a service, manual registration, `Render = false` |
| Render modes | Complex | Static + Server + WASM | Which islands share a hub, static reads, the cascade boundary |

```bash
dotnet run --project samples/RForge.Blazor.Events.Sample
```

## Building and testing

```bash
dotnet build src/RForge/RForge.sln
dotnet test  src/RForge/RForge.sln
```

| Project | Contents |
|---|---|
| `src/RForge/RForge.Blazor.Events` | Runtime library (`net8.0`, `net9.0`, `net10.0`). Packs the generator into `analyzers/dotnet/cs`. |
| `src/RForge/RForge.Blazor.Events.Generators` | Incremental source generator + analyzers (`netstandard2.0`) |
| `tests/RForge.Blazor.Events.Tests` | Routing, delivery, state, persistence and bUnit component tests |
| `tests/RForge.Blazor.Events.Generators.Tests` | Generated-code, incremental-caching and diagnostic tests |
| `samples/RForge.Blazor.Events.Sample(.Client)` | Example app |

**For contributors:** the samples and tests in this repository reference the source projects instead of the package. A project reference to the runtime doesn't bring the generator along, so they reference both:

```xml
<ProjectReference Include="path/to/src/RForge.Blazor.Events/RForge.Blazor.Events.csproj" />
<ProjectReference Include="path/to/src/RForge.Blazor.Events.Generators/RForge.Blazor.Events.Generators.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

## Tutorials

Step-by-step guides with complete, compile-checked code and the recommended file layout: [docs/events/tutorials](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/README.md).

| Start here | Then |
|---|---|
| [01 · Setup](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/01-setup.md): package, registration, project layout | [02 · State](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/02-state.md) · [03 · State with cascades](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/03-state-with-cascades.md) · [04 · Events](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/04-events.md) |
| [13 · Best practices](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/13-best-practices.md): one-page checklist | [05 · Scopes and routing](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/05-scopes-and-routing.md) · [06 · Scoped state](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/06-scoped-state.md) · [07 · Services](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/07-services-and-background-work.md) · [08 · Persistence](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/08-prerendering-and-persistence.md) · [09 · Render modes](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/09-render-modes.md) · [10 · Manual registration](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/10-manual-registration.md) · [11 · Testing](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/11-testing-with-bunit.md) · [12 · Troubleshooting](https://github.com/Rumhyek/RForge/blob/main/docs/events/tutorials/12-troubleshooting.md) |

## Design documentation

| Document | Covers |
|---|---|
| [Design overview](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/index.md) | Goals, non-goals, glossary, architecture, open questions, verified framework behaviors |
| [API](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/api.md) | Public API surface |
| [Events](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/events.md) | Publishing, routing, delivery semantics |
| [State](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/state.md) | Stores, selectors, providers, persistence |
| [Scoping](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/scoping.md) | `EventScope`, `StateScope`, resolution rules |
| [Components](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/components.md) | `RfEventComponent` lifecycle, manual registration |
| [Source generator](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/source-generator.md) | Attributes, generated code, diagnostics |
| [Render modes](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/render-modes.md) | Behavior per render mode, limitations |
| [Decisions](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/decisions.md) | Architecture decision log |
| [Comparison](https://github.com/Rumhyek/RForge/blob/main/docs/events/design/comparison.md) | vs. Fluxor, `CascadingValueSource<T>`, BlazorComponentBus, Svelte |
