# 08 · Prerendering and persistence

**Goal:** load state once during prerendering, show it in the prerendered HTML, and keep it when the page becomes interactive, with no second load and no flash of empty content.

*See it running:* the **Checkout** sample page (`/examples/checkout`). Reload it and read the note under the cart.

## The problem

A prerendered interactive page is rendered **twice**:

1. **Prerender:** on the server, in a throwaway request scope with its **own** hub. The HTML is sent to the browser.
2. **Interactive:** a new circuit (or the WebAssembly runtime) starts with a **new** hub, starting from `Initial`.

So state loaded in step 1 is gone in step 2. The component loads again, and the UI may flash back to the initial value in between. Persistence carries chosen root state across.

## 1. Define the state

```csharp
// MyApp.Client/Features/Profile/ProfileState.cs
using System.Text.Json.Serialization;

namespace MyApp.Client.Features.Profile;

public sealed record ProfileState(string? DisplayName, string? Email, DateTime? LoadedAt) : IState<ProfileState>
{
    public static ProfileState Initial => new(null, null, null);

    /// <summary>Derived values aren't persisted; they're recomputed after restore.</summary>
    [JsonIgnore]
    public bool IsLoaded => LoadedAt is not null;
}
```

## 2. Add JSON metadata

Persistence serializes state to JSON. For trimmed or AOT WebAssembly apps, use a source-generated `JsonSerializerContext`:

```csharp
// MyApp.Client/AppJsonContext.cs
using System.Text.Json.Serialization;
using MyApp.Client.Features.Profile;

namespace MyApp.Client;

/// <summary>One context for every persisted state type in the app.</summary>
[JsonSerializable(typeof(ProfileState))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
```

## 3. Opt the state into persistence

```csharp
// MyApp.Client/EventHubSetup.cs (excerpt)
public static void Configure(RfEventOptions options)
{
    options.PersistState(AppJsonContext.Default.ProfileState);
}
```

`options.PersistState<ProfileState>()` (no argument) also works. It uses reflection-based JSON and is marked `[RequiresUnreferencedCode]`, so prefer the `JsonTypeInfo` overload in anything that runs in WebAssembly.

## 4. Load only if needed

The loader must not overwrite restored data. Check first:

```csharp
// MyApp.Client/Features/Profile/Components/ProfileLoader.cs
namespace MyApp.Client.Features.Profile.Components;

/// <summary>Invisible component that loads the profile once, during prerendering if the page prerenders.</summary>
public sealed class ProfileLoader : RfEventComponent
{
    protected override async Task OnInitializedAsync()
    {
        var profile = State<ProfileState>();
        if (profile.Value.IsLoaded)
        {
            return; // restored from the prerender (or loaded earlier in this session)
        }

        await Task.Delay(100); // stand-in for an API or database call
        profile.Set(new ProfileState("Ada Lovelace", "ada@example.com", DateTime.Now));
    }
}
```

```razor
@* MyApp.Client/Features/Profile/Components/ProfileCard.razor *@
@inherits RfEventComponent

@if (Profile.IsLoaded)
{
    <p>Signed in as <strong>@Profile.DisplayName</strong> (@Profile.Email)</p>
    <small class="text-body-secondary">Loaded at @Profile.LoadedAt?.ToLongTimeString() (now rendering in @RendererInfo.Name)</small>
}
else
{
    <p>Loading…</p>
}
```

```csharp
// MyApp.Client/Features/Profile/Components/ProfileCard.razor.cs
namespace MyApp.Client.Features.Profile.Components;

public partial class ProfileCard
{
    [UseState]
    private partial ProfileState Profile { get; }
}
```

```razor
@* MyApp.Client/Pages/ProfilePage.razor *@
@page "/profile"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Profile.Components

<ProfileLoader />
<ProfileCard />
```

Open `/profile`, then view the page source. The prerendered HTML already contains *"Signed in as Ada Lovelace"*. When the page becomes interactive, the time shown doesn't change: the data came from the prerender, not from a second load.

> `ProfileCard` renders **before** `ProfileLoader` finishes its `await`, yet the prerendered HTML is still correct. During prerendering, Blazor keeps rendering until async initialization settles, and hub subscriptions are active during that time, so `ProfileCard` re-renders when the profile arrives.

## How it works

- During prerendering, the hub registers a callback with `PersistentComponentState`. When the prerender ends, it serializes the current **root** value of each opted-in state into the page.
- When the interactive hub first creates that store, it takes the persisted value instead of `Initial`.
- The data is written for both the Server and the WebAssembly runtime, so it works with Server, WebAssembly and Auto pages.

## Limits

- **Only root state** is persisted. `StateScope` instances aren't, because there's no stable identity to match a prerendered scope to its interactive twin.
- **Events are never persisted**: they're fire-and-forget.
- **Persistence only bridges prerender → interactive.** It doesn't carry state from a Server-phase Auto page to a later WebAssembly-phase page. For that, reload from your API.

## ✅ Best practices

- **Keep one `AppJsonContext` in the Client project** listing every persisted state type, next to `EventHubSetup.cs`.
- **Persist only what's expensive to load** (API/database results). Cheap UI state can just start from `Initial`.
- **Always "load if not loaded".** Give the state a way to tell (`IsLoaded`, a timestamp, a non-null field).
- **Mark derived properties `[JsonIgnore]`** so only source data is serialized.
- **Load in one place** (a loader component or the page) rather than in every component that reads the state.
- **Disable prerendering** (`prerender: false`) for pages whose data is user-specific and cheap to fetch, or that start background work. Persistence is for when you want the prerendered HTML.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Data flashes to empty, then reappears | State not opted in | Add `options.PersistState(…)` in `EventHubSetup.Configure` |
| Data loads twice | Loader doesn't check for restored data | "Load if not loaded" (step 4) |
| Works on Server, not on WebAssembly | `PersistState` configured only in the server `Program.cs` | Configure in the shared `EventHubSetup` |
| Trimming warnings (IL2026) in the Client project | Using `PersistState<T>()` without metadata | Use `PersistState(AppJsonContext.Default.T)` |
| Deserialization fails after a deploy | Persisted shape changed between prerender and interactive | The hub logs the error and falls back to `Initial`. Keep persisted records backward compatible |

**Next:** [09 · Render modes](09-render-modes.md)
