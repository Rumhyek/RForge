# 03 · State with cascades

**Goal:** let components that **don't** derive from `RfEventComponent` (third-party components, components from another library, or ones you'd rather not change) read hub state through an ordinary `[CascadingParameter]`.

*See it running:* the **Plain components** sample page (`/examples/plain-components`).

## When to use this

| Consumer | How it reads state | Re-renders |
|---|---|---|
| Your own components | `[UseState]` ([tutorial 02](02-state.md)) | Only when the value (or selected part) changes |
| Components you can't or won't change | `[CascadingParameter]` (this tutorial) | On **every** change of that state |

Prefer `[UseState]` wherever you can. Cascades are the compatibility path.

## 1. Define the state

```csharp
// MyApp.Client/Features/Theme/ThemeState.cs
namespace MyApp.Client.Features.Theme;

public sealed record ThemeState(string Name, string Background, string Foreground) : IState<ThemeState>
{
    public static ThemeState Initial => Light;

    public static ThemeState Light { get; } = new("Light", "#ffffff", "#212529");
    public static ThemeState Dark { get; } = new("Dark", "#212529", "#f8f9fa");
    public static ThemeState Sepia { get; } = new("Sepia", "#f4ecd8", "#5b4636");
}
```

## 2. A plain consumer

This component knows nothing about the hub. It's the kind of component you'd get from a UI library:

```razor
@* MyApp.Client/Features/Theme/Components/ThemedPanel.razor *@
<div class="p-3 rounded" style="background:@Theme?.Background; color:@Theme?.Foreground">
    @ChildContent
</div>

@code {
    [CascadingParameter]
    public ThemeState? Theme { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
```

## 3. Cascade the root state

Turn on `CascadeRootState<T>()` in the shared configuration:

```csharp
// MyApp.Client/EventHubSetup.cs (excerpt)
public static void Configure(RfEventOptions options)
{
    options.CascadeRootState<ThemeState>();
}
```

The root `ThemeState` is now also a cascading value available everywhere, through a `CascadingValueSource<ThemeState>` that the hub updates on every change. Because `EventHubSetup` is shared, both the server and the WebAssembly host get it.

## 4. Write the state

Writers are ordinary event-aware components:

```razor
@* MyApp.Client/Features/Theme/Components/ThemeSwitcher.razor *@
@inherits RfEventComponent

<div class="btn-group btn-group-sm">
    @foreach (var theme in Themes)
    {
        <button class="btn @(theme.Name == Current ? "btn-dark" : "btn-outline-dark")" @onclick="() => Pick(theme)">@theme.Name</button>
    }
</div>
```

```csharp
// MyApp.Client/Features/Theme/Components/ThemeSwitcher.razor.cs
namespace MyApp.Client.Features.Theme.Components;

public partial class ThemeSwitcher
{
    private static readonly ThemeState[] Themes = [ThemeState.Light, ThemeState.Dark, ThemeState.Sepia];

    [UseState(Select = nameof(SelectName))]
    private partial string Current { get; }

    private static string SelectName(ThemeState theme) => theme.Name;

    // Writes to the NEAREST provider: the root, or an enclosing StateScope (step 6).
    private void Pick(ThemeState theme) => State<ThemeState>().Set(theme);
}
```

## 5. Writing from a plain component

A plain component can't call `State<T>()`, but it can inject the hub:

```razor
@* MyApp.Client/Features/Theme/Components/DarkModeToggle.razor *@
@inject IRfEventHub Hub

<label class="form-check form-switch">
    <input class="form-check-input" type="checkbox" checked="@(Theme?.Name == "Dark")" @onchange="Toggle" />
    Dark mode
</label>

@code {
    [CascadingParameter]
    public ThemeState? Theme { get; set; }

    // The cascaded value is read-only; writes go through the store.
    private void Toggle() =>
        Hub.State<ThemeState>().Update(t => t.Name == "Dark" ? ThemeState.Light : ThemeState.Dark);
}
```

`IRfEventHub` is the **root** hub. Inside a `StateScope` (next step), a plain component that needs to write to the *scoped* instance should take the cascaded scope instead:

```csharp
[CascadingParameter]
public IRfEventHubScope? Scope { get; set; }

private void Toggle() => (Scope ?? Hub).State<ThemeState>().Update(/* … */);
```

## 6. Cascade a subtree's own copy

`<StateScope CascadeValue="true">` gives a subtree its **own** instance of the state ([tutorial 06](06-scoped-state.md)) and cascades that instance too:

```razor
@* MyApp.Client/Pages/ThemePage.razor *@
@page "/theme"
@rendermode InteractiveWebAssembly
@using MyApp.Client.Features.Theme
@using MyApp.Client.Features.Theme.Components

<h3>Site theme (root)</h3>
<ThemeSwitcher />
<DarkModeToggle />
<ThemedPanel>Uses the root theme.</ThemedPanel>

<h3 class="mt-4">Preview (its own theme)</h3>
<StateScope TState="ThemeState" Initial="ThemeState.Sepia" CascadeValue="true">
    <ThemeSwitcher />
    <ThemedPanel>Uses the preview's theme. Changing it doesn't affect the site.</ThemedPanel>
</StateScope>
```

Inside the `StateScope`, both the event-aware `ThemeSwitcher` and the plain `ThemedPanel` see the scope's instance. Outside it, they see the root.

## How it works

- `CascadeRootState<T>()` registers a `CascadingValueSource<T>` through `AddCascadingValue` and calls `NotifyChangedAsync(newValue)` on every effective change.
- `<StateScope CascadeValue="true">` renders an extra, non-fixed `<CascadingValue>` around its children with the current value. Without the flag, it doesn't render one and costs nothing.
- Cascading-parameter consumers get the new value through `SetParametersAsync`, so they re-render on **every** change. There are no selectors on this path.

## ✅ Best practices

- **Use cascades only for components you can't put on `RfEventComponent`.** Your own components should use `[UseState]` with selectors.
- **Configure `CascadeRootState` in `EventHubSetup.Configure`**, never in only one `Program.cs`. Otherwise the cascade disappears when the page switches runtime.
- **Keep cascaded state small and slow-changing** (theme, culture, current user). A frequently changing state cascaded to many consumers re-renders all of them.
- **Write through the store**, never by trying to change the cascaded object. The cascade is read-only.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `[CascadingParameter]` is always `null` | `CascadeRootState<T>()` not configured, or `CascadeValue="true"` missing on the `StateScope` | Add it in `EventHubSetup.Configure`, or set the flag |
| Works on Server, `null` on WebAssembly | Configured in the server `Program.cs` only | Use the shared `EventHubSetup.Configure` |
| Plain component's toggle changes the wrong instance | It injected `IRfEventHub` (root) while inside a `StateScope` | Use the cascaded `IRfEventHubScope` |
| Many components re-render on every change | They read via cascade | Move them to `[UseState]` with a selector |

**Next:** [04 · Events](04-events.md)
