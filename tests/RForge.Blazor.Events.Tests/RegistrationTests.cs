using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.Events.Tests.Components;

namespace RForge.Blazor.Events.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void Root_hub_is_scoped()
    {
        var services = new ServiceCollection().AddRForgeEvents().BuildServiceProvider(validateScopes: true);

        using var first = services.CreateScope();
        using var second = services.CreateScope();

        Assert.Same(first.ServiceProvider.GetRequiredService<IRfEventHub>(), first.ServiceProvider.GetRequiredService<IRfEventHub>());
        Assert.NotSame(first.ServiceProvider.GetRequiredService<IRfEventHub>(), second.ServiceProvider.GetRequiredService<IRfEventHub>());
    }

    [Fact]
    public void Calling_AddRForgeEvents_twice_throws()
    {
        var services = new ServiceCollection().AddRForgeEvents();
        Assert.Throws<InvalidOperationException>(() => services.AddRForgeEvents());
    }

    [Fact]
    public void Max_publish_depth_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RfEventOptions { MaxPublishDepth = 0 });
    }

    [Fact]
    public void Root_state_can_be_cascaded_to_plain_components()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddRForgeEvents(o => o.CascadeRootState<CounterState>());
        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = ctx.Render<PlainConsumer>();
        Assert.Equal("0", cut.Find(".plain").TextContent);

        cut.InvokeAsync(() => ctx.Services.GetRequiredService<IRfEventHub>().State<CounterState>().Update(s => s with { Count = 4 }));

        cut.WaitForAssertion(() => Assert.Equal("4", cut.Find(".plain").TextContent));
    }
}
