using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Events.Tests.Components;

/// <summary>C#-only component (no .razor file) subscribed at the root, with a Task + CancellationToken handler.</summary>
public partial class RootPingListener : RfEventComponent
{
    [Inject]
    private EventLog Log { get; set; } = default!;

    [Subscribe(Scope = ScopeKind.Root)]
    private Task OnPing(Ping ping, CancellationToken cancellationToken)
    {
        Log.Add($"root:{ping.Value}");
        return Task.CompletedTask;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "root");
}
