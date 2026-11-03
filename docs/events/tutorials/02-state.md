# 02 · State

**Goal:** share a value between components anywhere in the tree, so it updates everywhere when it changes, and keep re-renders to the components that actually use the changed part.

State is the hub's version of a Svelte store: it **always has a current value**, so a component that appears later still sees it. If you only need to say "something happened", use an [event](04-events.md) instead.

*See it running:* the **Shared state** sample page (`/examples/shared-counter`).

## 1. Define the state

```csharp
// MyApp.Client/Features/Counter/CounterState.cs
namespace MyApp.Client.Features.Counter;

public sealed record CounterState(int Count, int Step) : IState<CounterState>
{
    public static CounterState Initial => new(Count: 0, Step: 1);
}
```

Three rules:
- **Implement `IState<TSelf>`** and provide `static Initial`. The compiler requires it. It's the value the store starts with, so no DI registration is needed.
- **Use a `record`.** The store compares old and new values with record equality and skips notifications when nothing changed. A `class` would notify on every write (analyzer `RFEVT013` warns about this).
- **Keep it immutable.** You change state by replacing the value, never by mutating it.

## 2. Write to it

Any `RfEventComponent` can write through `State<T>()`, which returns the store:

```razor
@* MyApp.Client/Features/Counter/Components/CounterButtons.razor *@
@inherits RfEventComponent

<div class="btn-group">
    <button class="btn btn-primary" @onclick="Increment">+ step</button>
    <button class="btn btn-outline-secondary" @onclick="Reset">Reset</button>
</div>
```

```csharp
// MyApp.Client/Features/Counter/Components/CounterButtons.razor.cs
namespace MyApp.Client.Features.Counter.Components;

public partial class CounterButtons
{
    // Update = atomic read-modify-write; safe even if several threads write at once (Blazor Server).
    private void Increment() => State<CounterState>().Update(s => s with { Count = s.Count + s.Step });

    // Set = replace the whole value.
    private void Reset() => State<CounterState>().Set(CounterState.Initial);
}
```

`CounterButtons` writes but doesn't read the state in its markup, so it doesn't re-render when the count changes. That's what you want for a component that only writes.

## 3. Read it reactively

```razor
@* MyApp.Client/Features/Counter/Components/CounterDisplay.razor *@
@inherits RfEventComponent

<p class="display-6">Count: @Counter.Count (step @Counter.Step)</p>
```

```csharp
// MyApp.Client/Features/Counter/Components/CounterDisplay.razor.cs
namespace MyApp.Client.Features.Counter.Components;

public partial class CounterDisplay
{
    [UseState]
    private partial CounterState Counter { get; }
}
```

`[UseState]` on a `partial` get-only property makes the property **always return the current value** and **re-renders the component whenever the value changes**.

## 4. Re-render less with a selector

A component that only uses part of the state shouldn't re-render when another part changes. Give `[UseState]` a selector: a static method that picks out what the component needs. The component re-renders only when the selected value changes.

```razor
@* MyApp.Client/Features/Counter/Components/StepPicker.razor *@
@inherits RfEventComponent

<label>
    Step
    <select class="form-select" value="@Step" @onchange="Change">
        <option value="1">1</option>
        <option value="5">5</option>
        <option value="10">10</option>
    </select>
</label>
```

```csharp
// MyApp.Client/Features/Counter/Components/StepPicker.razor.cs
using Microsoft.AspNetCore.Components;

namespace MyApp.Client.Features.Counter.Components;

/// <summary>Only re-renders when Step changes, not on every increment.</summary>
public partial class StepPicker
{
    [UseState(Select = nameof(SelectStep))]
    private partial int Step { get; }

    private static int SelectStep(CounterState state) => state.Step;

    private void Change(ChangeEventArgs e) =>
        State<CounterState>().Update(s => s with { Step = int.Parse((string)e.Value!) });
}
```

To select several values, return a **tuple**. Tuples compare by value, so the component re-renders only when one of the elements changes:

```razor
@* MyApp.Client/Features/Counter/Components/MilestoneBadge.razor *@
@inherits RfEventComponent

@if (Milestone.Reached)
{
    <span class="badge text-bg-success">Milestone @Milestone.Level reached</span>
}
```

```csharp
// MyApp.Client/Features/Counter/Components/MilestoneBadge.razor.cs
namespace MyApp.Client.Features.Counter.Components;

public partial class MilestoneBadge
{
    // Re-renders only when the milestone level changes: once every 100, not on every click.
    [UseState(Select = nameof(SelectMilestone))]
    private partial (bool Reached, int Level) Milestone { get; }

    private static (bool, int) SelectMilestone(CounterState state) => (state.Count >= 100, state.Count / 100);
}
```

Selector rules (checked by analyzer `RFEVT008`): the selector must be `static`, take the state type as its only parameter, and return the property's type. Tuple element names don't have to match.

## 5. Put it on a page

```razor
@* MyApp.Client/Pages/CounterPage.razor *@
@page "/counter-state"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Counter.Components

<CounterDisplay />
<MilestoneBadge />
<div class="d-flex gap-3 align-items-end">
    <CounterButtons />
    <StepPicker />
</div>
```

Click *+ step*: `CounterDisplay` updates on every click, `StepPicker` never re-renders, and `MilestoneBadge` re-renders only when crossing a multiple of 100.

## 6. Read state outside the markup

Generated `[UseState]` properties are ready **before** `OnInitialized` runs, so you can use them during initialization:

```csharp
// MyApp.Client/Features/Counter/Components/CounterSince.razor.cs
namespace MyApp.Client.Features.Counter.Components;

public partial class CounterSince
{
    private int _startedAt;

    [UseState]
    private partial CounterState Counter { get; }

    // Already holds the current value: registration runs before OnInitialized.
    protected override void OnInitialized() => _startedAt = Counter.Count;
}
```

```razor
@* MyApp.Client/Features/Counter/Components/CounterSince.razor *@
@inherits RfEventComponent

<small>+@(Counter.Count - _startedAt) since this component appeared</small>
```

For a one-off read where you don't want re-renders, use `State<CounterState>().Value`.

## 7. State that holds a collection

Use immutable collections and replace them on every change:

```csharp
// MyApp.Client/Features/Favorites/FavoritesState.cs
using System.Collections.Immutable;

namespace MyApp.Client.Features.Favorites;

public sealed record FavoritesState(ImmutableList<string> Items) : IState<FavoritesState>
{
    public static FavoritesState Initial => new([]);

    public bool Contains(string item) => Items.Contains(item);

    public FavoritesState Toggle(string item) =>
        this with { Items = Items.Contains(item) ? Items.Remove(item) : Items.Add(item) };
}
```

```csharp
// MyApp.Client/Features/Favorites/Components/FavoriteButton.razor.cs
using Microsoft.AspNetCore.Components;

namespace MyApp.Client.Features.Favorites.Components;

public partial class FavoriteButton
{
    [Parameter, EditorRequired]
    public string Item { get; set; } = "";

    [UseState]
    private partial FavoritesState Favorites { get; }

    private void Toggle() => State<FavoritesState>().Update(f => f.Toggle(Item));
}
```

```razor
@* MyApp.Client/Features/Favorites/Components/FavoriteButton.razor *@
@inherits RfEventComponent

<button class="btn btn-sm btn-link" @onclick="Toggle">@(Favorites.Contains(Item) ? "★" : "☆")</button>
```

Records compare collection members **by reference**. A new `ImmutableList` instance always counts as a change (which is why `Toggle` returning a new list notifies), and mutating a list in place is never seen. That's one more reason to keep state immutable.

## 8. Read state from a service

Services use the same stores through `IRfEventHub`:

```csharp
// MyApp.Client/Features/Counter/CounterLogger.cs
using Microsoft.Extensions.Logging;

namespace MyApp.Client.Features.Counter;

/// <summary>Scoped service that logs every counter change.</summary>
public sealed class CounterLogger : IDisposable
{
    private readonly IDisposable _subscription;

    public CounterLogger(IRfEventHub hub, ILogger<CounterLogger> logger) =>
        _subscription = hub.State<CounterState>().Subscribe(s => logger.LogInformation("Counter is now {Count}", s.Count));

    public void Dispose() => _subscription.Dispose();
}
```

Store callbacks run **synchronously on the writer's thread**. Keep them short and don't touch UI from them. Register the service as **Scoped** (see [tutorial 07](07-services-and-background-work.md)).

## How it works

- The **root hub** creates a store for each state type the first time it's requested, using `Initial`.
- `Set`/`Update` compare the new value to the old one. If they're equal, nothing happens. Otherwise subscribers are notified.
- Components are notified through their own `InvokeAsync`, so writing from any thread is safe, and several quick writes collapse into one render with the latest value.

## ✅ Best practices

- **One state type per concept, in its feature folder** (`Features/Counter/CounterState.cs`). Big "app state" records make every selector work harder and couple unrelated features.
- **Name states `…State`** so they're easy to tell apart from events.
- **Put domain logic on the record** (`FavoritesState.Toggle`) instead of in components. Every writer then applies the same rules.
- **Writers use `State<T>()`, readers use `[UseState]`.** Don't add `[UseState]` to a component that only writes: it would re-render for nothing.
- **Use a selector when a component reads one or two fields.** It's the cheapest re-render optimization available.
- **Use `ImmutableList`/`ImmutableDictionary` inside state**, and replace them on change.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Component doesn't update | It reads `State<T>().Value` in markup | Use `[UseState]` for anything rendered |
| Updates are ignored | State mutated in place (e.g. `list.Add`) | Replace the value: `Update(s => s with { … })` |
| Every write re-renders even when nothing changed | State is a `class` (`RFEVT013`) | Make it a `record` |
| `RFEVT008` | Selector isn't static, has the wrong parameter, or returns the wrong type | Match `static TProperty Select(TState state)` |

**Next:** [03 · State with cascades](03-state-with-cascades.md)
