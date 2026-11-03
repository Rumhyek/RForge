using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Events.Tests.Components;

/// <summary>Combines generated registration with the OnRegisterSubscriptions hook.</summary>
public partial class HookComponent : RfEventComponent
{
    [Inject]
    private EventLog Log { get; set; } = default!;

    [Subscribe(Render = false)]
    private void OnPing(Ping ping) => Log.Add($"hook-generated:{ping.Value}");

    partial void OnRegisterSubscriptions(SubscriptionBuilder builder) =>
        builder.On<Pong>(p => Log.Add($"hook-manual:{p.Text}"));

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "hook");
}
