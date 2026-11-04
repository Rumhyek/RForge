# 04 · Events

**Goal:** let a component anywhere in the tree say "this happened" or "please do this", and let any other component react, without passing parameters or callbacks between them.

*See it running:* the **Notifications** sample page (`/examples/notifications`).

## Events or state?

| Use an **event** when… | Use **state** ([tutorial 02](02-state.md)) when… |
|---|---|
| Something *happened* or is *requested*: "item added", "save requested", "show a toast" | There's a *current value*: "cart contents", "theme", "selected tab" |
| A listener that isn't there at the time **should** miss it | A component that appears later **must** see the latest value |
| Each occurrence matters, even if the payload repeats | Only the latest value matters, and equal values are no-ops |

A common pattern is to do both: update state *and* publish an event (the cart changes and an "item added" animation plays).

## 1. Define events

```csharp
// MyApp.Client/Features/Toasts/ToastEvents.cs
namespace MyApp.Client.Features.Toasts;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Danger,
}

/// <summary>A command: "please show this toast". Named …Requested.</summary>
public sealed record ToastRequested(string Message, ToastLevel Level = ToastLevel.Info) : IEvent;

/// <summary>A fact: "the user dismissed a toast". Named in the past tense.</summary>
public sealed record ToastDismissed(Guid Id) : IEvent;

/// <summary>A fact: "the user cleared every toast".</summary>
public sealed record ToastsCleared : IEvent;
```

Rules:
- **Implement `IEvent`.**
- **Use an immutable `sealed record`.** The same instance goes to every handler (analyzer `RFEVT012` flags mutable events).
- **Name facts in the past tense** (`ItemAdded`, `ToastDismissed`) and **commands with `…Requested`** (`ToastRequested`, `SaveRequested`). That makes it obvious whether a handler is reacting or being asked to act.

## 2. Publish

```razor
@* MyApp.Client/Features/Toasts/Components/SaveButton.razor *@
@inherits RfEventComponent

<button class="btn btn-primary" @onclick="SaveAsync">Save</button>
```

```csharp
// MyApp.Client/Features/Toasts/Components/SaveButton.razor.cs
namespace MyApp.Client.Features.Toasts.Components;

public partial class SaveButton
{
    private async Task SaveAsync()
    {
        await Task.Delay(200); // pretend to save
        await PublishAsync(new ToastRequested("Saved!", ToastLevel.Success));
    }
}
```

`PublishAsync` completes once every handler has run. Delivery is **sequential** and in a predictable order.

## 3. Handle with `[Subscribe]`

```razor
@* MyApp.Client/Features/Toasts/Components/ToastHost.razor *@
@inherits RfEventComponent

<div class="toast-stack">
    @foreach (var toast in _toasts)
    {
        <div class="alert alert-@toast.Level.ToString().ToLowerInvariant() d-flex" @key="toast.Id">
            <span class="me-auto">@toast.Message</span>
            <button class="btn-close" @onclick="() => DismissAsync(toast.Id)"></button>
        </div>
    }
    @if (_toasts.Count > 1)
    {
        <button class="btn btn-sm btn-link" @onclick="ClearAsync">Clear all</button>
    }
</div>
```

```csharp
// MyApp.Client/Features/Toasts/Components/ToastHost.razor.cs
namespace MyApp.Client.Features.Toasts.Components;

public partial class ToastHost
{
    private readonly List<Toast> _toasts = [];

    // Scope = Root: hear ToastRequested from anywhere, even from inside an EventScope (tutorial 05).
    [Subscribe(Scope = ScopeKind.Root)]
    private void OnToastRequested(ToastRequested request) =>
        _toasts.Add(new Toast(Guid.NewGuid(), request.Message, request.Level));

    private Task DismissAsync(Guid id)
    {
        _toasts.RemoveAll(t => t.Id == id);
        return PublishAsync(new ToastDismissed(id)).AsTask();
    }

    private Task ClearAsync()
    {
        _toasts.Clear();
        return PublishAsync(new ToastsCleared()).AsTask();
    }

    private sealed record Toast(Guid Id, string Message, ToastLevel Level);
}
```

The handler's **first parameter type** decides which event it receives. After the handler runs, the component re-renders automatically.

## 4. Supported handler shapes

```csharp
// MyApp.Client/Features/Toasts/Components/ToastAudit.razor.cs
namespace MyApp.Client.Features.Toasts.Components;

/// <summary>The supported [Subscribe] signatures, one per event type.</summary>
public partial class ToastAudit
{
    private readonly List<string> _log = [];
    private int _dismissed;

    // 1. Synchronous.
    [Subscribe]
    private void OnDismissed(ToastDismissed e) => _dismissed++;

    // 2. Asynchronous (Task or ValueTask).
    [Subscribe]
    private async Task OnCleared(ToastsCleared e)
    {
        await Task.Yield();
        _log.Add("all cleared");
    }

    // 3. Asynchronous with a CancellationToken. It's cancelled if the publisher cancels
    //    OR this component is disposed while the handler is still running.
    [Subscribe(Render = false)]
    private async ValueTask OnRequested(ToastRequested e, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);
        // Render = false: no automatic re-render, so call StateHasChanged yourself when needed.
        if (e.Level == ToastLevel.Danger)
        {
            _log.Add("danger toast seen");
            StateHasChanged();
        }
    }
}
```

```razor
@* MyApp.Client/Features/Toasts/Components/ToastAudit.razor *@
@inherits RfEventComponent

<small>Dismissed: @_dismissed · @string.Join(" | ", _log.TakeLast(3))</small>
```

| Signature | Notes |
|---|---|
| `void M(TEvent e)` | Simplest |
| `Task M(TEvent e)` / `ValueTask M(TEvent e)` | Async |
| `Task M(TEvent e, CancellationToken ct)` / `ValueTask …` | Async + cancellation |

Anything else (static, generic, extra parameters, other return types) gets analyzer error `RFEVT004`.

| `[Subscribe]` option | Default | Meaning |
|---|---|---|
| `Scope = ScopeKind.Root` | `Nearest` | Listen at the root instead of the nearest scope ([tutorial 05](05-scopes-and-routing.md)) |
| `ScopeName = "x"` | – | Listen at the nearest enclosing scope named `x` |
| `Render = false` | `true` | Skip the automatic `StateHasChanged` |

## 5. Put it together

```razor
@* MyApp.Client/Pages/ToastsPage.razor *@
@page "/toasts"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Toasts.Components

<ToastHost />

<div class="card">
    <div class="card-body">
        <div class="card">
            <div class="card-body">
                @* Deeply nested, with no parameters or callbacks passed down. *@
                <SaveButton />
            </div>
        </div>
    </div>
</div>

<ToastAudit />
```

## 6. When a handler throws

A handler's exception goes to the nearest `ErrorBoundary` around **that** component, as if it had thrown while rendering. The publisher and the other handlers aren't affected:

```razor
@* MyApp.Client/Pages/ToastsWithBoundary.razor *@
@page "/toasts-safe"
@rendermode InteractiveAuto
@using MyApp.Client.Features.Toasts.Components

<ToastHost />
<SaveButton />

<ErrorBoundary>
    <ChildContent><ToastAudit /></ChildContent>
    <ErrorContent>The audit panel failed; the rest of the page keeps working.</ErrorContent>
</ErrorBoundary>
```

## 7. Keep handlers fast

Handlers run one after another, and `PublishAsync` waits for all of them. A handler that awaits a 2-second animation delays every handler after it, *and* the publisher. Start long work without awaiting it:

```csharp
// MyApp.Client/Features/Toasts/Components/ToastBell.razor.cs
namespace MyApp.Client.Features.Toasts.Components;

public partial class ToastBell
{
    private bool _ringing;

    [Subscribe(Scope = ScopeKind.Root)]
    private void OnToastRequested(ToastRequested request)
    {
        _ringing = true;
        _ = StopRingingAsync(); // don't await: let delivery continue
    }

    private async Task StopRingingAsync()
    {
        try
        {
            await Task.Delay(1000, DisposalToken); // DisposalToken: cancelled when the component goes away
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _ringing = false;
        StateHasChanged();
    }
}
```

```razor
@* MyApp.Client/Features/Toasts/Components/ToastBell.razor *@
@inherits RfEventComponent

<span class="@(_ringing ? "text-warning" : "")">🔔</span>
```

## How it works

- `[Subscribe]` methods are wired up by the generated `RegisterSubscriptions` before `OnInitialized`, and unsubscribed when the component is disposed.
- Delivery goes through each component's `InvokeAsync`, so publishing from any thread is safe.
- By default a component listens at its **nearest scope** and a publish **bubbles** up from the publisher's nearest scope to the root. Without any `EventScope`s, everything is at the root, so every subscriber hears every publish of its event type.

## Best practices

- **Keep event types next to the feature that owns them** (`Features/Toasts/ToastEvents.cs`). Several small related events can share one `…Events.cs` file. Large ones get their own file.
- **Use past tense for facts and `…Requested` for commands.**
- **Carry only what handlers need** (ids, small values). Handlers can read anything else from state.
- **Don't use events for parent → direct child.** A `[Parameter]` or `EventCallback` is clearer there. The hub is for components that don't share a direct line.
- **Never `await` long work in a handler.** Fire and forget, using `DisposalToken`.
- **Wrap optional or fragile listeners in an `ErrorBoundary`.**

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Handler never runs | Listener mounts *after* the publish; events aren't replayed | Use state if late components must see it |
| Handler runs but UI doesn't update | `Render = false` without `StateHasChanged()` | Remove `Render = false`, or call `StateHasChanged()` |
| Clicking feels slow | A handler awaits long work | Fire and forget (step 7) |
| Handler in an `EventScope` doesn't hear a publish from outside | The publish doesn't travel *into* scopes by default | See [tutorial 05](05-scopes-and-routing.md) (`Route.Global`, `ScopeKind.Root`) |
| `RFEVT011` warning | Two handlers for the same event in one component | Merge them, unless you really want both |

**Next:** [05 · Scopes and routing](05-scopes-and-routing.md)
