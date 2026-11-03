using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Bus.Tests.Components;

/// <summary>Attaches to the nearest ancestor scope named "outer", with a ValueTask handler.</summary>
public partial class NamedPingListener : BusComponentBase
{
    [Inject]
    private EventLog Log { get; set; } = default!;

    [Subscribe(ScopeName = "outer")]
    private ValueTask OnPing(Ping ping)
    {
        Log.Add($"named:{ping.Value}");
        return ValueTask.CompletedTask;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "named");
}
