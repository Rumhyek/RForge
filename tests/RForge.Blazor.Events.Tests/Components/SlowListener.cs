using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Events.Tests.Components;

/// <summary>Handler that waits until cancelled, recording how it ended.</summary>
public partial class SlowListener : RfEventComponent
{
    [Inject]
    private EventLog Log { get; set; } = default!;

    [Parameter]
    public TaskCompletionSource Started { get; set; } = new();

    [Subscribe]
    private async Task OnPing(Ping ping, CancellationToken cancellationToken)
    {
        Started.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Add("slow:cancelled");
            throw;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "slow");
}
