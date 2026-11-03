using Microsoft.AspNetCore.Components;

namespace RForge.Blazor.Bus.Sample.Client.Examples.Widgets;

public partial class TodoHeader
{
    [Parameter]
    public EventCallback OnRemove { get; set; }

    [UseState(Select = nameof(SelectTitle))]
    private partial string Title { get; }

    private static string SelectTitle(TodoListState list) => list.Title;

    // Local: only subscribers attached to this widget's scope receive it. The page-level ActivityLog never sees it.
    private Task ClearCompleted() => PublishAsync(new ClearCompletedRequested(), Route.Local).AsTask();
}
