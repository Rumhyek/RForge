namespace RForge.Blazor.Events.Tests.Components;

/// <summary>Selector consumer: re-renders only when the label changes.</summary>
public partial class LabelView
{
    [UseState(Select = nameof(SelectLabel))]
    private partial string Label { get; }

    private static string SelectLabel(CounterState state) => state.Label;

    public int Renders { get; private set; }

    protected override void OnAfterRender(bool firstRender) => Renders++;
}
