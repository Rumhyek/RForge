# 09 · Render modes

**Goal:** know what the hub does in each render mode, and avoid the one boundary it can't cross.

*See it running:* the **Render modes** sample page (`/examples/render-modes`).

## The one rule

> **A hub belongs to a DI container. Components share a hub only when they share a container.**

| Where the component runs | Its hub | Shared with |
|---|---|---|
| Static SSR | One per HTTP request | Components rendered in that request |
| Interactive Server | One per circuit | **Every** Interactive Server component on the page, for that user |
| Interactive WebAssembly | One per browser app instance | **Every** WebAssembly component on the page |
| Interactive Auto | Server's or WebAssembly's, depending on which the instance landed on | That runtime's components |
| Prerendering | One per request (thrown away afterward) | The prerender only. See [tutorial 08](08-prerendering-and-persistence.md) |

## What works in each mode

| Mode | Events | State | Notes |
|---|---|---|---|
| Static SSR | ✅ within the request | ✅ within the request | Nothing updates after the HTML is sent |
| Interactive Server | ✅ | ✅ | Publishing from background threads is safe |
| Interactive WebAssembly | ✅ | ✅ | Generated code is trim/AOT-safe |
| Interactive Auto | ✅ per runtime | ✅ per runtime | State doesn't move from Server to WebAssembly |

## 1. Register in both hosts

If any component runs in WebAssembly (WebAssembly or Auto render mode), the client needs its own hub:

```csharp
// MyApp.Client/Program.cs (excerpt)
builder.Services.AddRForgeEvents(EventHubSetup.Configure);
```

```csharp
// MyApp/Program.cs (excerpt)
builder.Services.AddRForgeEvents(EventHubSetup.Configure);
```

Same `EventHubSetup.Configure` in both, so `CascadeRootState`, `PersistState` and error handling match ([tutorial 01](01-setup.md#3-create-one-shared-configuration)).

## 2. Mixed islands don't talk to each other

On a page with both a Server island and a WebAssembly island, each talks to a **different** hub:

```razor
@* MyApp/Components/Pages/IslandsPage.razor *@
@page "/islands"
@using MyApp.Client.Features.Welcome.Components

@* Static page hosting two islands. *@
<Greeting @rendermode="InteractiveServer" />          @* circuit hub *@
<NameEditor @rendermode="InteractiveServer" />        @* same circuit hub: shares with the Greeting above *@
<NameEditor @rendermode="InteractiveWebAssembly" />   @* WebAssembly hub: typing here does NOT update the Greeting *@
```

This is a deliberate limitation ([ADR-007](../design/decisions.md#adr-007-no-js-and-no-cross-island-bridge)). Bridging would require JavaScript interop or a server round trip.

What to do instead:
- **Use one interactive render mode per area that has to communicate.** Most apps already do: a global render mode on `<Routes>`, or one per page.
- If two different-mode islands really must coordinate, use an app-level channel (an API, a SignalR hub, browser storage), and translate it into hub events on each side.

## 3. Declare scopes inside the island

Cascading values don't cross from static rendering into an interactive island, and scopes are cascading values:

```razor
@* MyApp/Components/Pages/ScopeBoundaryPage.razor *@
@page "/scope-boundary"
@using MyApp.Client.Features.Settings.Components

@* ❌ This EventScope is rendered statically. The island inside can't see it, so
      SettingsPanel's children end up attached to the circuit's root hub instead. *@
<EventScope Name="outer">
    <SettingsPanel @rendermode="InteractiveServer" />
</EventScope>

@* ✅ SettingsPanel declares its own <EventScope Name="settings"> inside itself,
      so the scope lives in the island. *@
<SettingsPanel @rendermode="InteractiveServer" />
```

Rule of thumb: **a component that needs a scope declares it itself** (as in [tutorial 05](05-scopes-and-routing.md)), so it doesn't matter where the component is placed. If your whole app is interactive (render mode set on `<Routes>`), scopes in layouts work too.

## 4. Auto: same component, two possible hubs

An `InteractiveAuto` page runs on Server until the WebAssembly runtime is downloaded, then on WebAssembly for later visits. Each visit gets a fresh hub in whichever runtime it lands on:

- Root state isn't carried from the Server phase to the WebAssembly phase. Reload data from its source (see "load if not loaded" in [tutorial 08](08-prerendering-and-persistence.md#4-load-only-if-needed)).
- Server-only services (`MyApp/Services`) aren't available in WebAssembly. Pages that need them should be `InteractiveServer`.

## ✅ Best practices

- **Choose one interactive render mode per page region**, ideally per page or app-wide.
- **Keep event-aware code in the Client project** so it can run anywhere. Put only server-only pages and services in the server project.
- **Let features declare their own scopes**, so render-mode boundaries never cut a feature in half.
- **Turn off prerendering** (`prerender: false`) where a page starts background work or the prerendered HTML isn't useful.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Two parts of a page don't update each other | They're different render modes (different hubs) | Use the same render mode |
| `No registered service of type 'IRfEventHub'` in the browser | Hub not registered in `MyApp.Client/Program.cs` | Register it ([tutorial 01](01-setup.md#4-register-the-hub-in-both-hosts)) |
| A scope "doesn't work" in an island | The scope was declared in static markup outside the island | Declare it inside the interactive component |
| State resets when Auto switches to WebAssembly | New runtime, new hub | Reload the data, or keep it in your backend |

**Next:** [10 · Manual registration](10-manual-registration.md)
