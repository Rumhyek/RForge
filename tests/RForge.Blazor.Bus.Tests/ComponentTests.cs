using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.Bus.Tests.Components;

namespace RForge.Blazor.Bus.Tests;

public sealed class ComponentTests : BunitContext
{
    private readonly EventLog _log = new();

    public ComponentTests()
    {
        Services.AddSingleton(_log);
        Services.AddBlazorBus();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private IBus Bus => Services.GetRequiredService<IBus>();

    // ── [UseState] ───────────────────────────────────────────────────────

    [Fact]
    public void UseState_renders_current_value_and_rerenders_on_change()
    {
        var cut = Render<CounterView>();
        Assert.Equal("0", cut.Find("#count").TextContent);

        cut.InvokeAsync(() => Bus.State<CounterState>().Update(s => s with { Count = 3 }));

        Assert.Equal("3", cut.Find("#count").TextContent);
    }

    [Fact]
    public void Setting_an_equal_value_does_not_rerender()
    {
        var cut = Render<CounterView>();
        var renders = cut.Instance.Renders;

        cut.InvokeAsync(() => Bus.State<CounterState>().Set(new CounterState(0, "start")));

        Assert.Equal(renders, cut.Instance.Renders);
    }

    [Fact]
    public void Selector_rerenders_only_when_projection_changes()
    {
        var cut = Render<LabelView>();
        var renders = cut.Instance.Renders;
        var store = Bus.State<CounterState>();

        cut.InvokeAsync(() => store.Update(s => s with { Count = s.Count + 1 }));
        Assert.Equal(renders, cut.Instance.Renders);

        cut.InvokeAsync(() => store.Update(s => s with { Label = "changed" }));
        Assert.Equal(renders + 1, cut.Instance.Renders);
        Assert.Equal("changed", cut.Find("#label").TextContent);
    }

    [Fact]
    public async Task State_written_off_the_renderer_thread_is_marshaled()
    {
        var cut = Render<CounterView>();

        await Task.Run(() => Bus.State<CounterState>().Update(s => s with { Count = 42 }));

        cut.WaitForAssertion(() => Assert.Equal("42", cut.Find("#count").TextContent));
    }

    // ── [Subscribe] ──────────────────────────────────────────────────────

    [Fact]
    public async Task Subscribe_handler_runs_and_rerenders()
    {
        var cut = Render<PingListener>(p => p.Add(x => x.Id, "a"));

        await cut.InvokeAsync(() => Bus.PublishAsync(new Ping(7)).AsTask());

        Assert.Equal(["a:7"], _log.Entries);
        Assert.Equal("1", cut.Find(".received").TextContent);
    }

    [Fact]
    public async Task Disposed_component_stops_receiving()
    {
        Render<PingListener>(p => p.Add(x => x.Id, "a"));
        await DisposeComponentsAsync();

        await Bus.PublishAsync(new Ping(1));

        Assert.Empty(_log.Entries);
    }

    [Fact]
    public async Task Handler_exception_goes_to_error_boundary_not_publisher()
    {
        var cut = Render<ErrorHost>();

        await cut.InvokeAsync(() => Bus.PublishAsync(new Ping(9)).AsTask());

        Assert.Equal("boom 9", cut.Find(".error").TextContent);
        Assert.Equal(["survivor:9"], _log.Entries);
    }

    [Fact]
    public async Task Disposing_component_cancels_running_handler()
    {
        var started = new TaskCompletionSource();
        Render<SlowListener>(p => p.Add(x => x.Started, started));

        var publish = Bus.PublishAsync(new Ping(1)).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await DisposeComponentsAsync();
        await publish.WaitAsync(TimeSpan.FromSeconds(5)); // completes without throwing

        Assert.Equal(["slow:cancelled"], _log.Entries);
    }

    [Fact]
    public async Task Render_false_handlers_and_partial_hook_both_register()
    {
        var cut = Render<HookComponent>();
        var renders = cut.RenderCount;

        await cut.InvokeAsync(() => Bus.PublishAsync(new Ping(1)).AsTask());
        await cut.InvokeAsync(() => Bus.PublishAsync(new Pong("x")).AsTask());

        Assert.Equal(["hook-generated:1", "hook-manual:x"], _log.Entries);
        Assert.Equal(renders + 1, cut.RenderCount); // only the hook's handler (render: true) re-rendered
    }

    [Fact]
    public async Task Manual_registration_api_works()
    {
        var cut = Render<ManualComponent>();

        await cut.InvokeAsync(() => Bus.PublishAsync(new Ping(2)).AsTask());
        await cut.InvokeAsync(() => Bus.PublishAsync(new Pong("p")).AsTask());
        await cut.InvokeAsync(cut.Instance.Increment);

        Assert.Equal(["manual-sync:2", "manual-async:2", "manual-pong:p"], _log.Entries);
        Assert.Equal("count=1", cut.Markup);

        var renders = cut.Instance.Renders;
        await cut.InvokeAsync(() => cut.Instance.Relabel("other")); // selector only watches Count
        Assert.Equal(renders, cut.Instance.Renders);
    }

    [Fact]
    public void Builder_cannot_be_used_after_registration()
    {
        SubscriptionBuilder? captured = null;
        var cut = Render<CapturingComponent>(p => p.Add(x => x.Capture, b => captured = b));

        Assert.NotNull(captured);
        Assert.Throws<InvalidOperationException>(() => captured!.On<Ping>(_ => { }));
        Assert.NotNull(cut.Instance);
    }

    // ── Non-interactive rendering ────────────────────────────────────────

    [Fact]
    public void Prerendered_component_sees_state_set_later_in_the_same_render()
    {
        // During prerendering the renderer keeps rendering until async initialization settles, so a component
        // rendered *before* the state was loaded must still update (the bug the checkout sample exposed).
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<LateLoaderHost>();

        cut.WaitForAssertion(() => Assert.Equal("9", cut.Find("#count").TextContent));
    }

    // ── Scopes ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EventScope_routes_bubble_up_and_not_sideways()
    {
        var cut = Render<ScopedTree>();

        await cut.InvokeAsync(() => cut.Instance.InnerPublisher.Publish(1).AsTask());

        // Root-scope subscribers run in subscription (render) order: RootPingListener precedes PingListener "top".
        Assert.Equal(["inner:1", "outer:1", "named:1", "root:1", "top:1"], _log.Entries);
    }

    [Fact]
    public async Task EventScope_local_and_broadcast_and_global()
    {
        var cut = Render<ScopedTree>();
        var publisher = cut.Instance.InnerPublisher;

        await cut.InvokeAsync(() => publisher.Publish(1, Route.Local).AsTask());
        Assert.Equal(["inner:1"], _log.Entries);

        await cut.InvokeAsync(() => publisher.Publish(2, Route.Named("outer", RouteDirection.Broadcast)).AsTask());
        Assert.Equal(["inner:1", "outer:2", "named:2", "inner:2", "sibling:2"], _log.Entries);
    }

    [Fact]
    public async Task Root_bus_global_reaches_every_scope()
    {
        var cut = Render<ScopedTree>();

        await cut.InvokeAsync(() => Bus.PublishAsync(new Ping(3), Route.Global).AsTask());

        Assert.Equal(["root:3", "top:3", "outer:3", "named:3", "inner:3", "sibling:3"], _log.Entries);
    }

    [Fact]
    public async Task Removing_an_EventScope_disposes_its_scope()
    {
        var cut = Render<ScopedTree>();
        var scope = BusScope.From(cut.Instance.InnerPublisher.CurrentScope);

        await DisposeComponentsAsync();

        Assert.True(scope.IsDisposed);
    }

    [Fact]
    public void StateScopes_isolate_state_instances()
    {
        var cut = Render<TwoCounters>();

        Assert.Equal("0", cut.Find("#a #count").TextContent);
        Assert.Equal("10", cut.Find("#b #count").TextContent);
        Assert.Equal("0", cut.Find("#root #count").TextContent);

        cut.InvokeAsync(cut.Instance.A.Increment);
        cut.InvokeAsync(cut.Instance.B.Increment);
        cut.InvokeAsync(cut.Instance.B.Increment);

        Assert.Equal("1", cut.Find("#a #count").TextContent);
        Assert.Equal("12", cut.Find("#b #count").TextContent);
        Assert.Equal("0", cut.Find("#root #count").TextContent);
        Assert.Equal(0, Bus.State<CounterState>().Value.Count);
    }

    [Fact]
    public void StateScope_cascade_value_updates_plain_components()
    {
        var cut = Render<CascadeHost>();
        Assert.Equal("0", cut.Find(".plain").TextContent);

        cut.InvokeAsync(cut.Instance.Pub.Increment);

        Assert.Equal("1", cut.Find(".plain").TextContent);
    }

    /// <summary>Exposes its builder so the test can try to use it after registration.</summary>
    public sealed class CapturingComponent : BusComponentBase
    {
        [Parameter]
        public Action<SubscriptionBuilder>? Capture { get; set; }

        protected override void RegisterSubscriptions(SubscriptionBuilder builder) => Capture?.Invoke(builder);
    }
}
