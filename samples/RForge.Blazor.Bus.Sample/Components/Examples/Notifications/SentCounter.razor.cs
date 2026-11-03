namespace RForge.Blazor.Bus.Sample.Components.Examples.Notifications;

/// <summary>A state reader. Whenever it appears, it shows the current value, including changes made while it was hidden.</summary>
public partial class SentCounter
{
    [UseState]
    private partial NotificationStats Stats { get; }
}
