using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.Events.Tests.Components;

namespace RForge.Blazor.Events.Tests;

[JsonSerializable(typeof(CounterState))]
internal sealed partial class TestJsonContext : JsonSerializerContext;

public sealed class PersistenceTests
{
    private const string Key = "RForge.Blazor.Events:RForge.Blazor.Events.Tests.CounterState";

    [Fact]
    public void Root_state_is_persisted_when_the_prerender_ends()
    {
        using var ctx = new BunitContext();
        var persisted = ctx.AddBunitPersistentComponentState();
        ctx.Services.AddRForgeEvents(o => o.PersistState(TestJsonContext.Default.CounterState));
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        ctx.Render<CounterView>();
        ctx.Services.GetRequiredService<IRfEventHub>().State<CounterState>().Set(new CounterState(7, "loaded"));
        persisted.TriggerOnPersisting();

        Assert.True(persisted.TryTake<string>(Key, out var json));
        Assert.Contains("\"Count\":7", json);
    }

    [Fact]
    public void Root_state_is_restored_in_the_interactive_session()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddRForgeEvents(o => o.PersistState(TestJsonContext.Default.CounterState));
        var persisted = ctx.AddBunitPersistentComponentState();
        persisted.Persist(Key, """{"Count":12,"Label":"from-prerender"}""");
        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = ctx.Render<CounterView>();

        Assert.Equal("12", cut.Find("#count").TextContent);
        Assert.Equal("from-prerender", cut.Find("#label").TextContent);
    }

    [Fact]
    public void State_types_not_opted_in_start_from_Initial()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddRForgeEvents();
        var persisted = ctx.AddBunitPersistentComponentState();
        persisted.Persist(Key, """{"Count":12,"Label":"ignored"}""");
        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = ctx.Render<CounterView>();

        Assert.Equal("0", cut.Find("#count").TextContent);
    }
}
