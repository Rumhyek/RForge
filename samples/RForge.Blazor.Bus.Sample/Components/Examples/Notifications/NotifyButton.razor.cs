using Microsoft.AspNetCore.Components;

namespace RForge.Blazor.Bus.Sample.Components.Examples.Notifications;

/// <summary>Publishes an event (fire-and-forget) and also bumps a piece of state (has a current value).</summary>
public partial class NotifyButton
{
    [Parameter]
    public string Label { get; set; } = "Notify";

    [Parameter]
    public string Message { get; set; } = "Hello from deep in the tree";

    [Parameter]
    public ToastLevel Level { get; set; }

    private string Css => Level switch
    {
        ToastLevel.Success => "success",
        ToastLevel.Warning => "warning",
        ToastLevel.Danger => "danger",
        _ => "primary",
    };

    private async Task NotifyAsync()
    {
        State<NotificationStats>().Update(s => s with { Sent = s.Sent + 1 });
        await PublishAsync(new ToastRequested(Message, Level));
    }
}
