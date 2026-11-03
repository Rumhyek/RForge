# 01 · Setup

**Goal:** add RForge.Blazor.Bus to a Blazor Web App and build a first component that uses it.

This tutorial uses a Blazor Web App with **Auto** interactivity, which has a server project (`MyApp`) and a client project (`MyApp.Client`). That covers every render mode. If your app only uses Interactive Server, you have a single project: put the "Client" files in it and skip the client-specific steps.

## 1. Lay out the project

The bus needs a few files in specific places. This is the layout used throughout the tutorials:

```
MyApp/                                  server project (Blazor Web App host)
├── Program.cs                          registers the bus for the server
├── Services/                           server-only services (Scoped) that publish or subscribe
└── Components/                         layout, App.razor, server-only pages
MyApp.Client/                           everything that might run in WebAssembly
├── Program.cs                          registers the bus for the WebAssembly runtime
├── BusSetup.cs                         ONE bus configuration shared by both Program.cs files
├── AppJsonContext.cs                   JSON metadata for persisted state (tutorial 08)
├── GlobalUsings.cs                     global using RForge.Blazor.Bus;
├── _Imports.razor                      @using RForge.Blazor.Bus
├── Features/
│   └── <Feature>/
│       ├── <Feature>State.cs           IState records
│       ├── <Feature>Events.cs          IEvent records
│       └── Components/
│           ├── Something.razor         markup, starts with @inherits BusComponentBase
│           └── Something.razor.cs      code-behind with [Subscribe] / [UseState]
└── Pages/                              routable pages that run in WebAssembly / Auto
```

Why these locations:

| File | Location | Why |
|---|---|---|
| State and event types | `MyApp.Client/Features/…` | WebAssembly and Auto components need them, and the server project references the client, so both sides can use them. Types placed in the server project are invisible to WebAssembly. |
| Components that use the bus | `MyApp.Client/Features/<Feature>/Components/` | Same reason. The server can still render them with `InteractiveServer`. Keeping them next to their state and events makes a feature easy to find, move or delete. |
| `BusSetup.cs` | `MyApp.Client/` | The server and the WebAssembly runtime each have their **own** bus. One shared configuration method keeps them identical. |
| Server-only services | `MyApp/Services/` | Anything using databases, secrets or server APIs must not ship to the browser. |

## 2. Reference the package

Add the [`RForge.Blazor.Bus`](https://www.nuget.org/packages/RForge.Blazor.Bus) package to **both** projects:

```bash
dotnet add MyApp.Client package RForge.Blazor.Bus --version 0.0.1-rc4
dotnet add MyApp package RForge.Blazor.Bus --version 0.0.1-rc4
```

or in each `.csproj`:

```xml
<!-- MyApp.Client/MyApp.Client.csproj and MyApp/MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="RForge.Blazor.Bus" Version="0.0.1-rc4" />
</ItemGroup>
```

The package contains the runtime library **and** the source generator/analyzers, so nothing else is needed. It's a pre-release, so in the Visual Studio package manager tick **Include prerelease** to find it.

**Why both projects?** The runtime library would reach the server through its reference to `MyApp.Client`, but NuGet doesn't pass a package's **analyzers** (which include the source generator) through project references. Any project that contains components using `[Subscribe]` or `[UseState]` needs its own reference. Referencing the package in both projects also keeps them on the same version.

> **Tip:** with several projects, use [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management) (`Directory.Packages.props`) so the version is defined once.

## 3. Create one shared configuration

```csharp
// MyApp.Client/BusSetup.cs
namespace MyApp.Client;

/// <summary>Bus options shared by the server and the WebAssembly host, so both buses behave the same.</summary>
public static class BusSetup
{
    public static void Configure(BusOptions options)
    {
        // Later tutorials add options here, for example:
        //   options.CascadeRootState<ThemeState>();               (tutorial 03)
        //   options.PersistState(AppJsonContext.Default.X);       (tutorial 08)
    }
}
```

## 4. Register the bus in both hosts

```csharp
// MyApp/Program.cs
using MyApp.Client;
using MyApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// One root bus per circuit (Server) and per request (static rendering / prerendering).
builder.Services.AddBlazorBus(BusSetup.Configure);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);

app.Run();
```

```csharp
// MyApp.Client/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The WebAssembly runtime has its own DI container, so it needs its own bus.
builder.Services.AddBlazorBus(BusSetup.Configure);

await builder.Build().RunAsync();
```

> Forgetting the client registration is the most common setup mistake. Everything works on Server, then fails with *"no registered service of type IBus"* when a page switches to WebAssembly.

## 5. Import the namespace

`.razor` files and `.cs` files have separate imports, so add both:

```razor
@* MyApp.Client/_Imports.razor *@
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using MyApp.Client
@using RForge.Blazor.Bus
```

```csharp
// MyApp.Client/GlobalUsings.cs
global using RForge.Blazor.Bus;
```

In the server project, add `@using RForge.Blazor.Bus` to `MyApp/Components/_Imports.razor` if server components use the bus, and `using RForge.Blazor.Bus;` at the top of server services (or a `GlobalUsings.cs` there too). The client's global using doesn't apply to the server project.

## 6. Build a first bus-aware component

Two components that share no parent: one edits a name, the other greets it.

```csharp
// MyApp.Client/Features/Welcome/WelcomeState.cs
namespace MyApp.Client.Features.Welcome;

public sealed record WelcomeState(string Name) : IState<WelcomeState>
{
    public static WelcomeState Initial => new("stranger");
}
```

```razor
@* MyApp.Client/Features/Welcome/Components/NameEditor.razor *@
@inherits BusComponentBase

<input class="form-control" placeholder="Your name" @bind="_name" @bind:event="oninput" @bind:after="Save" />
```

```csharp
// MyApp.Client/Features/Welcome/Components/NameEditor.razor.cs
namespace MyApp.Client.Features.Welcome.Components;

public partial class NameEditor
{
    private string _name = "";

    private void Save() => State<WelcomeState>().Set(new WelcomeState(_name.Length == 0 ? "stranger" : _name));
}
```

```razor
@* MyApp.Client/Features/Welcome/Components/Greeting.razor *@
@inherits BusComponentBase

<h2>Hello, @Welcome.Name!</h2>
```

```csharp
// MyApp.Client/Features/Welcome/Components/Greeting.razor.cs
namespace MyApp.Client.Features.Welcome.Components;

public partial class Greeting
{
    [UseState]
    private partial WelcomeState Welcome { get; }
}
```

```razor
@* MyApp.Client/Pages/WelcomePage.razor *@
@page "/welcome"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Welcome.Components

<Greeting />
<hr />
<NameEditor />
```

Run the app and open `/welcome`. Typing in the box updates the greeting, although neither component knows the other exists.

## 7. Check that the generator ran

`[UseState] private partial WelcomeState Welcome { get; }` has no body. The source generator writes it. To see the generated code:

- **Visual Studio / Rider:** *Dependencies → Analyzers → RForge.Blazor.Bus.Generators → …Greeting.Bus.g.cs*
- **Any editor:** add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to the csproj and look under `obj/…/generated/`.

If the generator isn't running, the build fails with **CS9248** ("partial property must have an implementation part"). That almost always means the project containing the component has no `RForge.Blazor.Bus` package reference of its own (see [step 2](#2-reference-the-package)).

## How it works

- `AddBlazorBus` registers a **Scoped** root bus: one per circuit on Server, one per app instance in WebAssembly.
- `BusComponentBase` finds the bus, runs the generated `RegisterSubscriptions` before your `OnInitialized`, re-renders the component when state it reads changes, and unsubscribes on dispose.
- State types are created lazily from their `Initial` value the first time anything asks for them. No registration is needed.

## ✅ Best practices

- **Put state, events and bus-aware components in the Client project.** Then they work in every render mode.
- **Use one `BusSetup.Configure` for both hosts.** Two hand-written configurations drift apart.
- **One feature folder per concept** (`Features/Cart`, `Features/Welcome`). State, events and components that change together live together.
- **Always pair `.razor` with `.razor.cs`** for bus-aware components, even small ones. The attributes only work in the code-behind, and consistency means nobody has to remember which components are special.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `CS0263: Partial declarations … must not specify different base classes` | Base class declared only in the code-behind | Add `@inherits BusComponentBase` to the `.razor` file |
| `RFBUS001` | `[Subscribe]`/`[UseState]` inside an `@code` block | Move the member to the `.razor.cs` file |
| `RFBUS003` | Attributes used in a component that doesn't derive from `BusComponentBase` | Add `@inherits BusComponentBase` |
| `CS9248` on a `[UseState]` property | The project has no `RForge.Blazor.Bus` package reference of its own, so the generator didn't run | Add the package to that project ([step 2](#2-reference-the-package)) |
| Works on Server, fails on WebAssembly | Bus not registered in `MyApp.Client/Program.cs` | See [step 4](#4-register-the-bus-in-both-hosts) |

**Next:** [02 · State](02-state.md)
