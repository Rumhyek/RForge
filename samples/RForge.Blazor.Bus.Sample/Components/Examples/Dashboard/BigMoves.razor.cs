namespace RForge.Blazor.Bus.Sample.Components.Examples.Dashboard;

/// <summary>Render = false: the handler sees every tick but decides for itself when a render is worth it.</summary>
public partial class BigMoves
{
    private const decimal Threshold = 0.02m;

    private readonly List<string> _moves = [];
    private int _inspected;
    private int _renders;

    [Subscribe(Render = false)]
    private void OnTick(PriceTick tick)
    {
        _inspected++;

        var before = tick.Price - tick.Change;
        if (before == 0 || Math.Abs(tick.Change / before) < Threshold)
        {
            return;
        }

        _moves.Insert(0, $"{tick.At:HH:mm:ss} {tick.Symbol} {(tick.Change > 0 ? "jumped" : "dropped")} {Math.Abs(tick.Change / before):P1}");
        if (_moves.Count > 6)
        {
            _moves.RemoveAt(_moves.Count - 1);
        }

        StateHasChanged();
    }
}
