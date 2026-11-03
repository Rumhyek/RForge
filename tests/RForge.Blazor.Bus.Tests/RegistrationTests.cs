using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.Bus.Tests.Components;

namespace RForge.Blazor.Bus.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void Root_bus_is_scoped()
    {
        var services = new ServiceCollection().AddBlazorBus().BuildServiceProvider(validateScopes: true);

        using var first = services.CreateScope();
        using var second = services.CreateScope();

        Assert.Same(first.ServiceProvider.GetRequiredService<IBus>(), first.ServiceProvider.GetRequiredService<IBus>());
        Assert.NotSame(first.ServiceProvider.GetRequiredService<IBus>(), second.ServiceProvider.GetRequiredService<IBus>());
    }

    [Fact]
    public void Calling_AddBlazorBus_twice_throws()
    {
        var services = new ServiceCollection().AddBlazorBus();
        Assert.Throws<InvalidOperationException>(() => services.AddBlazorBus());
    }

    [Fact]
    public void Max_publish_depth_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BusOptions { MaxPublishDepth = 0 });
    }

    [Fact]
    public void Root_state_can_be_cascaded_to_plain_components()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddBlazorBus(o => o.CascadeRootState<CounterState>());
        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = ctx.Render<PlainConsumer>();
        Assert.Equal("0", cut.Find(".plain").TextContent);

        cut.InvokeAsync(() => ctx.Services.GetRequiredService<IBus>().State<CounterState>().Update(s => s with { Count = 4 }));

        cut.WaitForAssertion(() => Assert.Equal("4", cut.Find(".plain").TextContent));
    }
}
