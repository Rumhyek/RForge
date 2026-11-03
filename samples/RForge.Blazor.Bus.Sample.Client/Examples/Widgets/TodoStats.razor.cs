namespace RForge.Blazor.Bus.Sample.Client.Examples.Widgets;

/// <summary>Tuple selector: re-renders only when the done/total counts change, not when an item is renamed or reordered.</summary>
public partial class TodoStats
{
    private int _renders;

    [UseState(Select = nameof(SelectProgress))]
    private partial (int Done, int Total) Progress { get; }

    private static (int Done, int Total) SelectProgress(TodoListState list) =>
        (list.Items.Count(static i => i.Done), list.Items.Count);
}
