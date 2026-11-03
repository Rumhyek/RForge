# 11 · Testing with bUnit

**Goal:** unit-test event-aware components: render them, change state, publish events, and assert on the result.

The examples test components from the [State](02-state.md), [Events](04-events.md), [Scoped state](06-scoped-state.md) and [Persistence](08-prerendering-and-persistence.md) tutorials. The patterns apply to any component.

## 1. Create the test project

```xml
<!-- MyApp.Tests/MyApp.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="2.11.3" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApp.Client\MyApp.Client.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

The components are already compiled (with generated code) in `MyApp.Client`, and the `RForge.Blazor.Events` runtime reaches the tests through that project reference. Add a `RForge.Blazor.Events` package reference to the test project only if it defines event-aware test components of its own that use `[Subscribe]` or `[UseState]`.

## 2. A base class for hub tests

Every test needs the same three things: a hub, a render mode, and a way to reach the hub:

```csharp
// MyApp.Tests/EventTestContext.cs
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.Events;

namespace MyApp.Tests;

public abstract class EventTestContext : BunitContext
{
    protected EventTestContext()
    {
        Services.AddRForgeEvents();

        // RfEventComponent reads RendererInfo; bUnit requires it to be set explicitly.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    /// <summary>The root hub the rendered components use.</summary>
    protected IRfEventHub Hub => Services.GetRequiredService<IRfEventHub>();
}
```

## 3. Test state

```csharp
// MyApp.Tests/CounterTests.cs
using Bunit;
using MyApp.Client.Features.Counter;
using MyApp.Client.Features.Counter.Components;
using RForge.Blazor.Events;

namespace MyApp.Tests;

public sealed class CounterTests : EventTestContext
{
    [Fact]
    public void Display_shows_the_initial_state()
    {
        var cut = Render<CounterDisplay>();

        Assert.Contains("Count: 0", cut.Markup);
    }

    [Fact]
    public void Seeded_state_is_shown_on_first_render()
    {
        Hub.State<CounterState>().Set(new CounterState(Count: 41, Step: 1)); // arrange before rendering

        var cut = Render<CounterDisplay>();

        Assert.Contains("Count: 41", cut.Markup);
    }

    [Fact]
    public async Task Display_rerenders_when_state_changes()
    {
        var cut = Render<CounterDisplay>();

        // Write through the renderer's dispatcher, as the app would.
        await cut.InvokeAsync(() => Hub.State<CounterState>().Update(s => s with { Count = 5 }));

        Assert.Contains("Count: 5", cut.Markup);
    }

    [Fact]
    public void Buttons_write_to_state()
    {
        var cut = Render<CounterButtons>();

        cut.Find("button.btn-primary").Click();
        cut.Find("button.btn-primary").Click();

        Assert.Equal(2, Hub.State<CounterState>().Value.Count);
    }

    [Fact]
    public async Task Writes_from_another_thread_are_rendered()
    {
        var cut = Render<CounterDisplay>();

        await Task.Run(() => Hub.State<CounterState>().Set(new CounterState(7, 1)));

        // The component re-renders asynchronously via InvokeAsync, so wait for it.
        cut.WaitForAssertion(() => Assert.Contains("Count: 7", cut.Markup));
    }
}
```

## 4. Test events

```csharp
// MyApp.Tests/ToastTests.cs
using Bunit;
using MyApp.Client.Features.Toasts;
using MyApp.Client.Features.Toasts.Components;
using RForge.Blazor.Events;

namespace MyApp.Tests;

public sealed class ToastTests : EventTestContext
{
    [Fact]
    public async Task Host_shows_published_toasts()
    {
        var cut = Render<ToastHost>();

        await cut.InvokeAsync(() => Hub.PublishAsync(new ToastRequested("Hello", ToastLevel.Success)).AsTask());

        Assert.Contains("Hello", cut.Find(".alert-success").TextContent);
    }

    [Fact]
    public async Task Dismissing_publishes_an_event()
    {
        var dismissed = new List<ToastDismissed>();
        using var spy = Hub.Subscribe<ToastDismissed>((e, _) =>
        {
            dismissed.Add(e);
            return ValueTask.CompletedTask;
        });
        var cut = Render<ToastHost>();
        await cut.InvokeAsync(() => Hub.PublishAsync(new ToastRequested("Bye")).AsTask());

        cut.Find(".btn-close").Click();

        Assert.Single(dismissed);
        Assert.Empty(cut.FindAll(".alert"));
    }
}
```

A **spy subscription** (`Hub.Subscribe` in the test) is the easiest way to assert that a component *published* something.

## 5. Test scoped state

```csharp
// MyApp.Tests/PollTests.cs
using Bunit;
using MyApp.Client.Features.Polls;
using MyApp.Client.Features.Polls.Components;
using RForge.Blazor.Events;

namespace MyApp.Tests;

public sealed class PollTests : EventTestContext
{
    [Fact]
    public void Votes_stay_inside_their_card()
    {
        var cut = Render<PollCard>(p => p.Add(x => x.Poll, PollState.Create("Lunch?", "Pizza", "Sushi")));

        cut.FindAll("button").First(b => b.TextContent.StartsWith("Pizza")).Click();

        Assert.Contains("1 vote(s)", cut.Markup);                 // the card's own instance changed
        Assert.Equal(0, Hub.State<PollState>().Value.TotalVotes); // the root instance didn't
    }
}
```

## 6. Test prerendering and persistence

Simulate a prerendered page by marking the renderer as non-interactive, and fake the persisted data with bUnit's persistent-state double. **Register services before calling `Persist`**: bUnit locks the service collection at that point.

```csharp
// MyApp.Tests/ProfileTests.cs
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Client.Features.Profile;
using MyApp.Client.Features.Profile.Components;
using RForge.Blazor.Events;

namespace MyApp.Tests;

public sealed class ProfileTests
{
    [Fact]
    public void Card_shows_profile_loaded_later_in_the_same_prerender()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddRForgeEvents();
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false)); // like a prerender

        var cut = ctx.Render<ProfileLoaderAndCard>();

        cut.WaitForAssertion(() => Assert.Contains("Ada Lovelace", cut.Markup));
    }

    [Fact]
    public void Restored_profile_is_not_loaded_again()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddRForgeEvents(o => o.PersistState<ProfileState>()); // reflection JSON is fine in tests
        var persisted = ctx.AddBunitPersistentComponentState();
        persisted.Persist(
            "RForge.Blazor.Events:MyApp.Client.Features.Profile.ProfileState", // key: RForge.Blazor.Events:{FullName}
            """{"DisplayName":"Restored","Email":"r@example.com","LoadedAt":"2026-01-01T00:00:00"}""");
        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = ctx.Render<ProfileLoaderAndCard>();

        Assert.Contains("Restored", cut.Markup); // the loader saw IsLoaded and skipped its own load
    }
}

/// <summary>Test host rendering the loader and the card together, as the page does.</summary>
public sealed class ProfileLoaderAndCard : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<ProfileLoader>(0);
        builder.CloseComponent();
        builder.OpenComponent<ProfileCard>(1);
        builder.CloseComponent();
    }
}
```

## ✅ Best practices

- **One `BunitContext` per test** (xUnit creates a new test-class instance per test, so inheriting from `EventTestContext` gives you that for free). Each context has its own hub, so tests can't leak state into each other.
- **Arrange state through `Hub.State<T>()` before rendering** rather than clicking through the UI to get there.
- **Change state and publish inside `cut.InvokeAsync`**, which is how the app's renderer would run it.
- **Use `WaitForAssertion` for off-thread or delayed work.** Don't use `Thread.Sleep`.
- **Assert on markup for readers, and on spies or stores for writers.**
- **Put the test project next to the app** (`MyApp.Tests/`) with one test class per feature (`CounterTests`, `ToastTests`, …).

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `MissingRendererInfoException` | `SetRendererInfo` not called | Call it in the test base constructor |
| `No registered service of type 'IRfEventHub'` | `Services.AddRForgeEvents()` missing | Add it before the first `Render` |
| `InvalidOperationException` when calling `AddRForgeEvents` after `Persist` | bUnit locked the services | Register services first, then `Persist` |
| Assertion runs before the re-render | Write happened off the renderer | Use `cut.InvokeAsync(...)` or `WaitForAssertion` |

**Next:** [12 · Troubleshooting](12-troubleshooting.md)
