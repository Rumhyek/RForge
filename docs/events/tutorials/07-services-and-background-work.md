# 07 · Services and background work

**Goal:** publish events and write state from code that isn't a component, such as a server service, a timer or a SignalR callback, and let components update safely.

*See it running:* the **Live dashboard** sample page (`/examples/live-dashboard`).

## What's different outside components

| | In a `BusComponentBase` | In a service |
|---|---|---|
| Get the bus | `PublishAsync`, `State<T>()` helpers | Inject `IBus` (the root) |
| Subscribe | `[Subscribe]`, `[UseState]` | `bus.Subscribe<T>(…)`, `store.Subscribe(…)`, and **dispose the result** |
| Thread | The renderer's | Whatever thread you're on. **Publishing from any thread is safe**: components marshal through their own `InvokeAsync` |

## 1. Define what the service produces

```csharp
// MyApp.Client/Features/Clock/ClockState.cs
namespace MyApp.Client.Features.Clock;

public sealed record ClockState(DateTime Now, bool Running) : IState<ClockState>
{
    public static ClockState Initial => new(DateTime.MinValue, false);
}

/// <summary>Published once per minute boundary.</summary>
public sealed record MinutePassed(DateTime At) : IEvent;
```

These live in the **Client** project, so components in any render mode can use them, even though the service that produces them is server-only.

## 2. Write a Scoped service that publishes from a timer

```csharp
// MyApp/Services/ClockService.cs
using MyApp.Client.Features.Clock;
using RForge.Blazor.Bus;

namespace MyApp.Services;

/// <summary>Server-only, Scoped (one per circuit). Ticks on a thread-pool thread, never the renderer's.</summary>
public sealed class ClockService(IBus bus) : IDisposable
{
    private readonly Lock _gate = new();
    private Timer? _timer;
    private int _lastMinute = -1;

    public void Start()
    {
        lock (_gate)
        {
            _timer ??= new Timer(_ => _ = TickAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }

        bus.State<ClockState>().Update(s => s with { Running = false });
    }

    private async Task TickAsync()
    {
        try
        {
            var now = DateTime.Now;
            bus.State<ClockState>().Set(new ClockState(now, Running: true));

            if (now.Minute != _lastMinute)
            {
                _lastMinute = now.Minute;
                await bus.PublishAsync(new MinutePassed(now));
            }
        }
        catch (ObjectDisposedException)
        {
            // The circuit (and its bus) ended between ticks.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
```

Register it as **Scoped**:

```csharp
// MyApp/Program.cs (excerpt)
builder.Services.AddScoped<ClockService>();
```

> **Never register a service that holds `IBus` as Singleton.** On Blazor Server, the bus is per circuit, so a singleton would capture one user's bus and share it with everyone. In Development, DI scope validation reports this as an error at startup.

## 3. Components just read

```razor
@* MyApp.Client/Features/Clock/Components/ClockFace.razor *@
@inherits BusComponentBase

<span class="font-monospace fs-3">@(Clock.Running ? Clock.Now.ToString("HH:mm:ss") : "--:--:--")</span>
```

```csharp
// MyApp.Client/Features/Clock/Components/ClockFace.razor.cs
namespace MyApp.Client.Features.Clock.Components;

/// <summary>No InvokeAsync anywhere: the base class marshals updates from the timer thread.</summary>
public partial class ClockFace
{
    [UseState]
    private partial ClockState Clock { get; }
}
```

```razor
@* MyApp.Client/Features/Clock/Components/MinuteLog.razor *@
@inherits BusComponentBase

<ul class="small">@foreach (var at in _minutes) { <li>@at.ToString("HH:mm")</li> }</ul>
```

```csharp
// MyApp.Client/Features/Clock/Components/MinuteLog.razor.cs
namespace MyApp.Client.Features.Clock.Components;

public partial class MinuteLog
{
    private readonly List<DateTime> _minutes = [];

    [Subscribe]
    private void OnMinutePassed(MinutePassed e) => _minutes.Insert(0, e.At);
}
```

## 4. Start and stop from a server page

The page injects the server-only service, so it lives in the server project and uses `InteractiveServer`:

```razor
@* MyApp/Components/Pages/ClockPage.razor *@
@page "/clock"
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@implements IDisposable
@inject MyApp.Services.ClockService Clock
@using MyApp.Client.Features.Clock.Components

<ClockFace />
<MinuteLog />

@code {
    protected override void OnInitialized() => Clock.Start();

    public void Dispose() => Clock.Stop();
}
```

`prerender: false` keeps the timer from starting in the throwaway prerender scope ([tutorial 08](08-prerendering-and-persistence.md)).

## 5. Subscribe from a service

Services subscribe through `IBus` and must dispose the subscription:

```csharp
// MyApp/Services/ClockAuditor.cs
using MyApp.Client.Features.Clock;
using RForge.Blazor.Bus;

namespace MyApp.Services;

/// <summary>Scoped listener: logs every minute boundary for this circuit.</summary>
public sealed class ClockAuditor : IDisposable
{
    private readonly IDisposable _subscription;

    public ClockAuditor(IBus bus, ILogger<ClockAuditor> logger) =>
        _subscription = bus.Subscribe<MinutePassed>((e, cancellationToken) =>
        {
            logger.LogInformation("Minute passed at {At}", e.At);
            return ValueTask.CompletedTask;
        });

    public void Dispose() => _subscription.Dispose();
}
```

A Scoped service is only created when something resolves it. To have it listening for the whole circuit, resolve it early, for example with `@inject ClockAuditor Auditor` in a page or layout component that's always interactive.

Service handlers run **on the publisher's thread** (no marshaling: no renderer is involved). Keep them thread-safe.

## 6. Decide how service failures are reported

A throwing **component** handler always goes to its `ErrorBoundary`. For **service** handlers you choose:

```csharp
// MyApp.Client/BusSetup.cs (excerpt)
public static void Configure(BusOptions options)
{
    options.HandlerErrorMode = HandlerErrorMode.Log;   // default: log and continue
    // options.HandlerErrorMode = HandlerErrorMode.Throw; // run all, then throw AggregateException to the publisher
    options.MaxPublishDepth = 16;                      // nested publishes allowed before BusReentrancyException
}
```

`MaxPublishDepth` catches event loops (A's handler publishes B, B's handler publishes A, …) and stops them with `BusReentrancyException` instead of hanging.

## How it works

- `IBus` is the root scope of the current DI scope: per circuit on Server, per app instance in WebAssembly, per request when rendering statically.
- Store writes are serialized per store (`Update` is atomic). Component notifications are queued through each component's renderer.
- `PublishAsync` awaits every handler, including component handlers running on the renderer, so a service can rely on "all reactions ran" after the `await`.

## ✅ Best practices

- **Put server-only services in the server project** (`MyApp/Services/`), and keep the state and events they produce in the Client project.
- **Register bus-using services as Scoped.** Never Singleton.
- **Prefer state for continuous data** (prices, clocks, progress) and events for discrete moments (minute passed, job finished). State means a component that appears later still sees the current value.
- **Catch `ObjectDisposedException` in timers.** The circuit can end between ticks.
- **Stop background work when its page goes away** (`Dispose`/`DisposeAsyncCore`), and also in the service's own `Dispose`.
- **Turn prerendering off** for pages that start background work on initialization.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| One user sees another user's updates | Service registered as Singleton | Register as Scoped |
| `InvalidOperationException: … not associated with the Dispatcher` | Calling `StateHasChanged` from a service callback yourself | Let the bus do it: write state or publish, and let components react |
| Timer keeps running after leaving the page | Not stopped on dispose | Stop it in the page's `Dispose` and in the service's `Dispose` |
| `BusReentrancyException` | Handlers publish events that trigger each other | Break the loop, or check whether the change is needed before publishing |

**Next:** [08 · Prerendering and persistence](08-prerendering-and-persistence.md)
