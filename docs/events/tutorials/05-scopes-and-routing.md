# 05 · Scopes and routing

**Goal:** fence events into part of the page, so two copies of a widget don't hear each other, and choose exactly where each event travels.

*See it running:* the **Scoped widgets** sample page (`/examples/scoped-widgets`) and the **Checkout** page (`/examples/checkout`).

## The model in one picture

Without scopes there's one channel: every subscriber hears every publish of its event type. `<EventScope>` adds a node to a **scope tree**, and each component belongs to its **nearest** scope:

```
Root (the hub)
├── ActivityLog                ← listens at Root
└── EventScope "settings"
    ├── SettingsToolbar        ← publishes from "settings"
    ├── ProfileSection         ← listens at "settings"
    ├── NotificationSection    ← listens at "settings"
    └── SettingsStatus         ← listens at "settings"
```

- A **subscriber** listens at one scope: its nearest one by default.
- A **publish** starts at an origin scope (the publisher's nearest by default) and travels in a **direction**.

## Routes

| Route | Starts at | Goes | Reaches in the picture (publishing from "settings") |
|---|---|---|---|
| `Route.Bubble` *(default)* | Publisher's scope | Up to the root | settings listeners, then ActivityLog |
| `Route.Local` | Publisher's scope | Nowhere else | settings listeners only |
| `Route.Broadcast` | Publisher's scope | Down into child scopes | settings listeners, plus any scopes nested inside |
| `Route.Root` | Root | Nowhere else | ActivityLog only |
| `Route.Global` | Root | Down into **every** scope | everyone |
| `Route.Named("x")` | Nearest enclosing scope named `x` | Nowhere else (by default) | that scope's listeners |
| `Route.Named("x", RouteDirection.Broadcast)` | Nearest enclosing scope named `x` | Down | that scope and everything inside it |

Bubbling is the default because it matches DOM intuition: something happens inside a region, and the region and everything above it can react, but siblings don't hear it.

## 1. Define the events

```csharp
// MyApp.Client/Features/Settings/SettingsEvents.cs
namespace MyApp.Client.Features.Settings;

/// <summary>Command, broadcast down to every section inside the settings scope.</summary>
public sealed record SaveAllRequested : IEvent;

/// <summary>Fact, bubbles up from the section that saved.</summary>
public sealed record SectionSaved(string Section) : IEvent;
```

## 2. Create the scope

The component that owns the region declares the scope. The scope only affects the component's **children**, so the owner itself stays outside it:

```razor
@* MyApp.Client/Features/Settings/Components/SettingsPanel.razor *@
<EventScope Name="settings">
    <div class="card">
        <div class="card-header"><SettingsToolbar /></div>
        <div class="card-body">
            <ProfileSection />
            <NotificationSection />
        </div>
        <div class="card-footer"><SettingsStatus /> <SettingsHelp /></div>
    </div>
</EventScope>
```

`SettingsPanel` is a plain component: it only *creates* the scope. (`SettingsHelp` appears in [step 6](#6-choose-where-a-subscriber-listens).)

## 3. Broadcast a command into the scope

```razor
@* MyApp.Client/Features/Settings/Components/SettingsToolbar.razor *@
@inherits RfEventComponent

<button class="btn btn-primary btn-sm" @onclick="SaveAllAsync">Save all</button>
```

```csharp
// MyApp.Client/Features/Settings/Components/SettingsToolbar.razor.cs
namespace MyApp.Client.Features.Settings.Components;

public partial class SettingsToolbar
{
    // Find the enclosing "settings" scope and broadcast down through it.
    // A second SettingsPanel on the same page has its own scope and won't hear this.
    private Task SaveAllAsync() =>
        PublishAsync(new SaveAllRequested(), Route.Named("settings", RouteDirection.Broadcast)).AsTask();
}
```

`Route.Named` is more robust than `Route.Broadcast` here: if you later wrap the toolbar in its own scope, it still targets the panel.

## 4. React inside the scope, and bubble a fact back up

```razor
@* MyApp.Client/Features/Settings/Components/ProfileSection.razor *@
@inherits RfEventComponent

<p>Profile settings… @(_saving ? "(saving)" : "")</p>
```

```csharp
// MyApp.Client/Features/Settings/Components/ProfileSection.razor.cs
namespace MyApp.Client.Features.Settings.Components;

public partial class ProfileSection
{
    private bool _saving;

    [Subscribe] // nearest scope = "settings"
    private async Task OnSaveAll(SaveAllRequested request, CancellationToken cancellationToken)
    {
        _saving = true;
        StateHasChanged();
        await Task.Delay(300, cancellationToken); // pretend to save
        _saving = false;

        // Default route (Bubble): "settings", then the root.
        await PublishAsync(new SectionSaved("Profile"), cancellationToken: cancellationToken);
    }
}
```

```razor
@* MyApp.Client/Features/Settings/Components/NotificationSection.razor *@
@inherits RfEventComponent

<p>Notification settings…</p>
```

```csharp
// MyApp.Client/Features/Settings/Components/NotificationSection.razor.cs
namespace MyApp.Client.Features.Settings.Components;

public partial class NotificationSection
{
    [Subscribe]
    private Task OnSaveAll(SaveAllRequested request) => PublishAsync(new SectionSaved("Notifications")).AsTask();
}
```

```razor
@* MyApp.Client/Features/Settings/Components/SettingsStatus.razor *@
@inherits RfEventComponent

<small>Saved: @string.Join(", ", _saved)</small>
```

```csharp
// MyApp.Client/Features/Settings/Components/SettingsStatus.razor.cs
namespace MyApp.Client.Features.Settings.Components;

/// <summary>Inside the scope: hears only its own panel's sections.</summary>
public partial class SettingsStatus
{
    private readonly List<string> _saved = [];

    [Subscribe]
    private void OnSectionSaved(SectionSaved e) => _saved.Add(e.Section);
}
```

## 5. Listen outside the scope

```razor
@* MyApp.Client/Features/Settings/Components/ActivityLog.razor *@
@inherits RfEventComponent

<ul class="small">
    @foreach (var entry in _entries)
    {
        <li>@entry</li>
    }
</ul>
```

```csharp
// MyApp.Client/Features/Settings/Components/ActivityLog.razor.cs
namespace MyApp.Client.Features.Settings.Components;

/// <summary>Outside every scope, so it listens at the root and hears bubbled events from all panels.</summary>
public partial class ActivityLog
{
    private readonly List<string> _entries = [];

    [Subscribe]
    private void OnSectionSaved(SectionSaved e) => _entries.Add($"{DateTime.Now:T} saved {e.Section}");

    // Never fires: SaveAllRequested is broadcast DOWN from "settings", so it never reaches the root.
    [Subscribe]
    private void OnSaveAll(SaveAllRequested e) => _entries.Add("this line never appears");
}
```

```razor
@* MyApp.Client/Pages/SettingsPage.razor *@
@page "/settings"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Settings.Components

<div class="row">
    <div class="col"><SettingsPanel /></div>
    <div class="col"><SettingsPanel /></div>
</div>
<ActivityLog />
```

Click *Save all* in the left panel: only the left panel's sections save, only the left status updates, and the activity log records both sections.

## 6. Choose where a subscriber listens

By default a subscriber listens at its nearest scope. Override that per handler:

```csharp
// MyApp.Client/Features/Settings/Components/SettingsHelp.razor.cs
namespace MyApp.Client.Features.Settings.Components;

public partial class SettingsHelp
{
    private int _globalSaves;
    private int _panelSaves;

    // Listen at the root even when placed inside a scope: hears bubbled events from every panel.
    [Subscribe(Scope = ScopeKind.Root)]
    private void OnAnySectionSaved(SectionSaved e) => _globalSaves++;

    // Listen at the nearest enclosing scope named "settings", even if nested deeper inside other scopes.
    [Subscribe(ScopeName = "settings")]
    private void OnSaveAll(SaveAllRequested e) => _panelSaves++;
}
```

```razor
@* MyApp.Client/Features/Settings/Components/SettingsHelp.razor *@
@inherits RfEventComponent

<small>Saves everywhere: @_globalSaves · Save-all in this panel: @_panelSaves</small>
```

`ScopeName` lookups throw `RfEventScopeNotFoundException` if no enclosing scope has that name. That's deliberate, so a typo fails loudly instead of silently hearing nothing.

## How it works

- `<EventScope>` creates a child scope and cascades it with `IsFixed="true"`, so adding a scope costs nothing per descendant.
- Components resolve their nearest scope once, at initialization. Moving a component to another scope means re-creating it, which Blazor does anyway when markup changes.
- When an `EventScope` leaves the page, its scope and every subscription in it are disposed.

## ✅ Best practices

- **Declare the scope in the feature's root component** (`SettingsPanel`), not in a page or layout. The feature then works the same wherever it's placed.
- **Name scopes after the feature** (`"settings"`, `"checkout"`) and target them with `Route.Named`. That keeps working when the internal component structure changes.
- **Commands go down, facts go up**: broadcast `…Requested` into a scope, and bubble past-tense facts out of it.
- **Use `ScopeKind.Root` for app-wide listeners** (toasts, logs, analytics) so they hear events from inside every scope.
- **Scopes must be inside an interactive component.** A scope declared in a static layout doesn't reach interactive islands ([tutorial 09](09-render-modes.md)).

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| The component that renders `<EventScope>` doesn't hear events inside it | A scope only applies to its children | Move the handler into a child component |
| Root listener never hears a broadcast | `Broadcast` goes *down*, not up | Bubble a separate fact up, or listen inside the scope |
| Listener inside a scope misses events from elsewhere | Publishes bubble *up* to the root and don't enter other scopes | Publish with `Route.Global`, or listen with `ScopeKind.Root` |
| `RfEventScopeNotFoundException` | `Route.Named`/`ScopeName` used where no enclosing scope has that name | Check the name, and that the component is inside that scope |

**Next:** [06 · Scoped state](06-scoped-state.md)
