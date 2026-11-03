namespace RForge.Blazor.Events.Sample.Client.Examples.Widgets;

/// <summary>Lives outside every widget, so it's attached to the root and hears whatever bubbles up.</summary>
public partial class ActivityLog
{
    private readonly List<string> _entries = [];
    private int _clearSeen;

    [Subscribe]
    private void OnAdded(TodoAdded e) => Log($"➕ {e.List}: added “{e.Title}”");

    [Subscribe]
    private void OnCompleted(TodoCompleted e) => Log($"{(e.Done ? "✔" : "↺")} {e.List}: {(e.Done ? "completed" : "reopened")} “{e.Title}”");

    [Subscribe]
    private void OnReset(ResetAllRequested _) => Log("⟲ all lists reset (Route.Global)");

    // Never fires: ClearCompletedRequested is published with Route.Local inside a widget.
    [Subscribe]
    private void OnClearCompleted(ClearCompletedRequested _) => _clearSeen++;

    private void Log(string entry)
    {
        _entries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {entry}");
        if (_entries.Count > 12)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }
    }
}
