namespace RForge.Blazor.Events.Sample.Client.Examples.Widgets;

public partial class TodoInput
{
    private string _title = "";

    private async Task AddAsync()
    {
        var title = _title.Trim();
        if (title.Length == 0)
        {
            return;
        }

        _title = "";

        // State() resolves the nearest provider: this widget's StateScope, not the root.
        var list = State<TodoListState>();
        list.Update(s => s with { Items = s.Items.Add(new TodoItem(Guid.NewGuid(), title, false)) });

        // Bubble (default): this widget's scope, then each ancestor up to the root, where ActivityLog listens.
        await PublishAsync(new TodoAdded(list.Value.Title, title));
    }
}
