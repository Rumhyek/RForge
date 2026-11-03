namespace RForge.Blazor.Events.Tests.Components;

/// <summary>Whole-value [UseState] consumer. The .razor file has no @inherits; the base class is declared here.</summary>
public partial class CounterView : RfEventComponent
{
    [UseState]
    private partial CounterState Counter { get; }

    public int Renders { get; private set; }

    public CounterState Current => Counter;

    protected override void OnAfterRender(bool firstRender) => Renders++;
}
