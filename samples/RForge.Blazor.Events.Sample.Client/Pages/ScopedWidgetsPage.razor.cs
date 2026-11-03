namespace RForge.Blazor.Events.Sample.Client.Pages;

public partial class ScopedWidgetsPage
{
    private readonly List<string> _lists = ["Groceries", "Work", "Weekend"];
    private string _newList = "";

    private void AddList()
    {
        var name = _newList.Trim();
        if (name.Length > 0 && !_lists.Contains(name))
        {
            _lists.Add(name);
        }

        _newList = "";
    }

    // Global = (Root, Broadcast): starts at the root and visits every scope below it.
    private Task ResetAllAsync() => PublishAsync(new ResetAllRequested(), Route.Global).AsTask();
}
