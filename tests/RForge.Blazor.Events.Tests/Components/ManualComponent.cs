using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Events.Tests.Components;

/// <summary>Uses the manual registration API (no attributes, no generator).</summary>
public class ManualComponent : RfEventComponent
{
    private StateHandle<CounterState> _counter = default!;
    private SelectorHandle<CounterState, int> _count = default!;

    [Inject]
    private EventLog Log { get; set; } = default!;

    public int Count => _count.Value;

    public int Renders { get; private set; }

    public void Increment() => _counter.Update(s => s with { Count = s.Count + 1 });

    public void Relabel(string label) => _counter.Update(s => s with { Label = label });

    protected override void RegisterSubscriptions(SubscriptionBuilder builder)
    {
        _counter = builder.State<CounterState>(render: false);
        _count = builder.State<CounterState, int>(s => s.Count);

        // Overload resolution: sync lambda, async lambda, async lambda with token.
        builder.On<Ping>(e => Log.Add($"manual-sync:{e.Value}"));
        builder.On<Ping>(async e =>
        {
            await Task.Yield();
            Log.Add($"manual-async:{e.Value}");
        });
        builder.On<Pong>(async (e, ct) =>
        {
            await Task.Delay(1, ct);
            Log.Add($"manual-pong:{e.Text}");
        });
    }

    protected override void OnAfterRender(bool firstRender) => Renders++;

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, $"count={_count.Value}");
}
