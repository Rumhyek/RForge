namespace RForge.Blazor.Events.Sample.Components.Examples.Dashboard;

public partial class TickTape
{
    private readonly LinkedList<PriceTick> _ticks = new();

    // Published from a timer thread; the base class marshals this onto the renderer.
    [Subscribe]
    private void OnTick(PriceTick tick)
    {
        _ticks.AddFirst(tick);
        if (_ticks.Count > 10)
        {
            _ticks.RemoveLast();
        }
    }
}
