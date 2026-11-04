#nullable enable

using Microsoft.AspNetCore.Components;

namespace RForgeDocs.EventsExamples.Dashboard;

/// <summary>
/// Manual registration: the selector depends on the Symbol parameter, which a static [UseState] selector can't do.
/// Parameters are assigned before RegisterSubscriptions runs, so the lambda can capture it.
/// </summary>
public partial class TickerTile
{
    private SelectorHandle<MarketState, decimal> _price = default!;
    private decimal _lastChange;
    private int _renders;

    [Parameter, EditorRequired]
    public string Symbol { get; set; } = "";

    protected override void RegisterSubscriptions(SubscriptionBuilder builder)
    {
        // Re-render only when *this* symbol's price changes.
        _price = builder.State<MarketState, decimal>(m => m.Prices.GetValueOrDefault(Symbol));

        // Remember the direction of the last move without triggering a render of its own.
        builder.On<PriceTick>(tick =>
        {
            if (tick.Symbol == Symbol)
            {
                _lastChange = tick.Change;
            }
        }, render: false);
    }
}
