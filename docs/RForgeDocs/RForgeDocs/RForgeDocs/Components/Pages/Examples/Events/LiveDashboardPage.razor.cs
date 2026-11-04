using Microsoft.AspNetCore.Components;
using RForgeDocs.EventsExamples.Dashboard;

namespace RForgeDocs.Components.Pages.Examples.Events;

public partial class LiveDashboardPage
{
    private int _intervalMs = 250;

    [Inject]
    private MarketFeed Feed { get; set; } = default!;

    [UseState(Select = nameof(SelectSummary))]
    private partial (bool Running, long Ticks) Market { get; }

    private static (bool, long) SelectSummary(MarketState market) => (market.Running, market.Ticks);

    private void Start() => Feed.Start(TimeSpan.FromMilliseconds(_intervalMs));

    private void Stop() => Feed.Stop();

    protected override ValueTask DisposeAsyncCore()
    {
        Feed.Stop();
        return ValueTask.CompletedTask;
    }
}
