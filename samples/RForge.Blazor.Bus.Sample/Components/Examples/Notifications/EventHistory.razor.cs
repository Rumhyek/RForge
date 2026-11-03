namespace RForge.Blazor.Bus.Sample.Components.Examples.Notifications;

/// <summary>An event listener. Events published while it isn't mounted are gone for good.</summary>
public partial class EventHistory
{
    private readonly List<string> _received = [];

    [Subscribe]
    private void OnToast(ToastRequested toast) => _received.Add($"{toast.Level}: {toast.Message}");
}
