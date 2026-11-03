using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Bus.Tests.Components;

public partial class ThrowingListener : BusComponentBase
{
    [Subscribe]
    private void OnPing(Ping ping) => throw new InvalidOperationException($"boom {ping.Value}");

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "ok");
}
