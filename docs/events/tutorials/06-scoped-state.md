# 06 · Scoped state

**Goal:** put the same widget on a page several times, each with its **own** copy of a state type, while code inside the widget stays as simple as with global state.

*See it running:* the **Scoped widgets** sample page (`/examples/scoped-widgets`) and the right half of **Plain components**.

## The idea

By default every state type has one instance, at the root. `<StateScope TState="X">` creates a new instance of `X` for its subtree. Inside the subtree, anything that asks for `X` (`[UseState]`, `State<X>()`) gets the **nearest** instance:

```
Root                              PollState #root
├── PollCard "Lunch"
│   └── StateScope<PollState>     PollState #1  ← everything inside resolves here
└── PollCard "Offsite"
    └── StateScope<PollState>     PollState #2
```

Components inside a poll don't know or care which instance they get. That's the point.

## 1. Define the state

```csharp
// MyApp.Client/Features/Polls/PollState.cs
using System.Collections.Immutable;

namespace MyApp.Client.Features.Polls;

public sealed record PollState(string Question, ImmutableDictionary<string, int> Votes) : IState<PollState>
{
    public static PollState Initial => new("Untitled poll", ImmutableDictionary<string, int>.Empty);

    public static PollState Create(string question, params string[] options) =>
        new(question, options.ToImmutableDictionary(o => o, _ => 0));

    public int TotalVotes => Votes.Values.Sum();

    public PollState Vote(string option) => this with { Votes = Votes.SetItem(option, Votes[option] + 1) };
}
```

## 2. Wrap the widget in a `StateScope`

```razor
@* MyApp.Client/Features/Polls/Components/PollCard.razor *@
<StateScope TState="PollState" Initial="Poll">
    <div class="card mb-3">
        <div class="card-header"><PollQuestion /></div>
        <div class="card-body"><PollOptions /></div>
        <div class="card-footer"><PollTotal /></div>
    </div>
</StateScope>

@code {
    [Parameter, EditorRequired]
    public PollState Poll { get; set; } = default!;
}
```

`Initial` is optional. Without it, the scope starts from `PollState.Initial`. It's **read once**: changing the parameter later doesn't reset the poll. Write to the store instead.

## 3. Write the children as if the state were global

```razor
@* MyApp.Client/Features/Polls/Components/PollQuestion.razor *@
@inherits RfEventComponent

<strong>@Question</strong>
```

```csharp
// MyApp.Client/Features/Polls/Components/PollQuestion.razor.cs
namespace MyApp.Client.Features.Polls.Components;

public partial class PollQuestion
{
    [UseState(Select = nameof(SelectQuestion))]
    private partial string Question { get; }

    private static string SelectQuestion(PollState poll) => poll.Question;
}
```

```razor
@* MyApp.Client/Features/Polls/Components/PollOptions.razor *@
@inherits RfEventComponent

@foreach (var (option, votes) in Poll.Votes.OrderBy(v => v.Key))
{
    <button class="btn btn-outline-primary btn-sm me-2" @onclick="() => Vote(option)">@option (@votes)</button>
}
```

```csharp
// MyApp.Client/Features/Polls/Components/PollOptions.razor.cs
namespace MyApp.Client.Features.Polls.Components;

public partial class PollOptions
{
    [UseState]
    private partial PollState Poll { get; }

    // Resolves to this card's PollState, not the root's and not another card's.
    private void Vote(string option) => State<PollState>().Update(p => p.Vote(option));
}
```

```razor
@* MyApp.Client/Features/Polls/Components/PollTotal.razor *@
@inherits RfEventComponent

<small>@Total vote(s)</small>
```

```csharp
// MyApp.Client/Features/Polls/Components/PollTotal.razor.cs
namespace MyApp.Client.Features.Polls.Components;

public partial class PollTotal
{
    [UseState(Select = nameof(SelectTotal))]
    private partial int Total { get; }

    private static int SelectTotal(PollState poll) => poll.TotalVotes;
}
```

## 4. Use it several times

```razor
@* MyApp.Client/Pages/PollsPage.razor *@
@page "/polls"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Polls
@using MyApp.Client.Features.Polls.Components

<PollCard Poll="@PollState.Create("Lunch?", "Pizza", "Sushi", "Salad")" />
<PollCard Poll="@PollState.Create("Offsite?", "Beach", "Mountains")" />
```

Each card counts its own votes.

## 5. Reach past the scope

Sometimes a component inside a scope needs the **root** instance of the same type. Point resolution at the root:

```csharp
// MyApp.Client/Features/Polls/Components/FeaturedPollBadge.razor.cs
namespace MyApp.Client.Features.Polls.Components;

/// <summary>Placed inside a PollCard, but shows the app-wide featured poll held at the root.</summary>
public partial class FeaturedPollBadge
{
    [UseState(Select = nameof(SelectQuestion), Scope = ScopeKind.Root)]
    private partial string FeaturedQuestion { get; }

    [UseState(Select = nameof(SelectQuestion))]
    private partial string ThisQuestion { get; }

    private static string SelectQuestion(PollState poll) => poll.Question;
}
```

```razor
@* MyApp.Client/Features/Polls/Components/FeaturedPollBadge.razor *@
@inherits RfEventComponent

@if (FeaturedQuestion == ThisQuestion)
{
    <span class="badge text-bg-warning">Featured</span>
}
```

Types that **no** `StateScope` provides always fall back to the root, so you only need `ScopeKind.Root` when the same type exists at both levels. `ScopeName = "…"` works too, to start resolution at a named scope.

## 6. Scopes, events and cleanup

- **A `StateScope` is also an event scope.** For components inside it, it's their nearest scope: `Route.Local` from inside reaches only that scope's listeners, and `[Subscribe]` handlers listen there by default. Give it a `Name` to target it with `Route.Named` ([tutorial 05](05-scopes-and-routing.md)).
- **To provide several state types** to one subtree, nest `StateScope`s.
- **When the `StateScope` leaves the page** (e.g. the card is removed), its store is disposed along with all subscriptions to it.

```razor
@* MyApp.Client/Features/Polls/Components/PollCardWithNotes.razor *@
@* Two scoped state types for one widget: nest the scopes. *@
<StateScope TState="PollState" Name="poll" Initial="Poll">
    <StateScope TState="PollNotesState">
        <PollQuestion />
        <PollOptions />
    </StateScope>
</StateScope>

@code {
    [Parameter, EditorRequired]
    public PollState Poll { get; set; } = default!;
}
```

```csharp
// MyApp.Client/Features/Polls/PollNotesState.cs
namespace MyApp.Client.Features.Polls;

public sealed record PollNotesState(string Text) : IState<PollNotesState>
{
    public static PollNotesState Initial => new("");
}
```

## How it works

- `StateScope` creates a child scope, registers it as the **provider** of `TState`, and cascades the scope (fixed, so the cascade costs nothing).
- Resolution walks up from the component's nearest scope to the first provider of the type. Root provides every type implicitly.
- Each component resolves once, at initialization, so the lookup isn't repeated on every render.

## Best practices

- **Put the `StateScope` in the widget's root component** (`PollCard`), so callers just write `<PollCard />` and can't forget it.
- **Pass starting data through `Initial`** from the widget's parameters. Don't write to the store from the parent.
- **Write child components as if the state were global.** If a child needs to know it's scoped, the design is usually leaking.
- **Use selectors in scoped widgets too.** A list of 50 widgets that each re-render on every change adds up.
- **Nest scopes for multiple state types**, outermost first, so the structure reads top-down.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| All cards share one value | `StateScope` missing, so everything resolves to the root | Wrap the widget in `<StateScope TState="…">` |
| Changing `Initial` does nothing | It's read once at initialization (a warning is logged) | Write to the store |
| A component in the card sees the root value | It sits *outside* the `StateScope` element | Move it inside |
| `Route.Local` from inside the card misses a listener outside the `StateScope` | The `StateScope` is the nearest scope | Use `Route.Bubble` or `Route.Named` |

**Next:** [07 · Services and background work](07-services-and-background-work.md)
