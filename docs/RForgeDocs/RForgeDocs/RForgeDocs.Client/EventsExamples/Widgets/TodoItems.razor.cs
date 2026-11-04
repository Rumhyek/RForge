namespace RForgeDocs.Client.EventsExamples.Widgets;

public partial class TodoItems
{
    [UseState]
    private partial TodoListState List { get; }

    private async Task ToggleAsync(TodoItem item)
    {
        var toggled = item with { Done = !item.Done };
        State<TodoListState>().Update(s => s with { Items = s.Items.Replace(item, toggled) });
        await PublishAsync(new TodoCompleted(List.Title, item.Title, toggled.Done));
    }

    // Arrives only from this widget's header (Route.Local).
    [Subscribe]
    private void OnClearCompleted(ClearCompletedRequested _) =>
        State<TodoListState>().Update(s => s with { Items = s.Items.RemoveAll(static i => i.Done) });

    // Arrives from the page (Route.Global), which broadcasts from the root into every scope.
    [Subscribe]
    private void OnResetAll(ResetAllRequested _) =>
        State<TodoListState>().Update(s => s with { Items = [] });
}
