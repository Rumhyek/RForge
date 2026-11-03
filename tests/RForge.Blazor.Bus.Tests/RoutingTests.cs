namespace RForge.Blazor.Bus.Tests;

/// <summary>
/// The worked example from docs/events/design/events.md:
/// <code>
/// Root
/// ├── "checkout"            (A)
/// │   ├── "address"         (B)  ◀ publisher
/// │   │   └── (unnamed)     (C)
/// │   └── "payment"         (D)
/// └── "sidebar"             (E)
/// </code>
/// </summary>
public sealed class RoutingTests : IDisposable
{
    private readonly RootBus _root = new(new BusOptions());
    private readonly BusScope _a, _b, _c, _d, _e;
    private readonly List<string> _received = [];

    public RoutingTests()
    {
        _a = _root.CreateChild("checkout");
        _b = _a.CreateChild("address");
        _c = _b.CreateChild(null);
        _d = _a.CreateChild("payment");
        _e = _root.CreateChild("sidebar");

        Listen(_root, "Root");
        Listen(_a, "A");
        Listen(_b, "B");
        Listen(_c, "C");
        Listen(_d, "D");
        Listen(_e, "E");
    }

    public void Dispose() => _root.Dispose();

    private void Listen(BusScope scope, string name) =>
        scope.Subscribe<Ping>((_, _) =>
        {
            _received.Add(name);
            return ValueTask.CompletedTask;
        });

    public static TheoryData<string, string[]> Routes => new()
    {
        { "Bubble", ["B", "A", "Root"] },
        { "Local", ["B"] },
        { "Broadcast", ["B", "C"] },
        { "Root", ["Root"] },
        { "Global", ["Root", "A", "B", "C", "D", "E"] },
        { "Named(checkout)", ["A"] },
        { "Named(checkout, Broadcast)", ["A", "B", "C", "D"] },
        { "Named(checkout, Bubble)", ["A", "Root"] },
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Route_reaches_expected_scopes_in_order(string route, string[] expected)
    {
        var value = route switch
        {
            "Bubble" => Route.Bubble,
            "Local" => Route.Local,
            "Broadcast" => Route.Broadcast,
            "Root" => Route.Root,
            "Global" => Route.Global,
            "Named(checkout)" => Route.Named("checkout"),
            "Named(checkout, Broadcast)" => Route.Named("checkout", RouteDirection.Broadcast),
            "Named(checkout, Bubble)" => Route.Named("checkout", RouteDirection.Bubble),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };

        await _b.PublishAsync(new Ping(1), value);

        Assert.Equal(expected, _received);
    }

    [Fact]
    public void Default_route_is_bubble_and_default_target_is_nearest()
    {
        Assert.Equal(Route.Bubble, default);
        Assert.Equal(ScopeTarget.Nearest, default);
    }

    [Fact]
    public async Task Named_route_to_non_ancestor_throws()
    {
        await Assert.ThrowsAsync<BusScopeNotFoundException>(() => _b.PublishAsync(new Ping(1), Route.Named("sidebar")).AsTask());
        Assert.Empty(_received);
    }

    [Fact]
    public async Task Named_lookup_includes_the_scope_itself()
    {
        await _b.PublishAsync(new Ping(1), Route.Named("address"));
        Assert.Equal(["B"], _received);
    }

    [Fact]
    public async Task Publishing_through_the_root_bus_with_bubble_reaches_only_root()
    {
        await _root.PublishAsync(new Ping(1));
        Assert.Equal(["Root"], _received);
    }

    [Fact]
    public void Subscribing_to_a_missing_named_scope_throws()
    {
        Assert.Throws<BusScopeNotFoundException>(() =>
            _e.Subscribe<Ping>((_, _) => ValueTask.CompletedTask, ScopeTarget.Named("checkout")));
    }

    [Fact]
    public async Task Subscriptions_only_receive_their_event_type()
    {
        await _b.PublishAsync(new Pong("x"), Route.Global);
        Assert.Empty(_received);
    }

    [Fact]
    public async Task Disposed_scope_is_skipped_by_broadcast_and_rejects_use()
    {
        _c.Dispose();

        await _b.PublishAsync(new Ping(1), Route.Broadcast);

        Assert.Equal(["B"], _received);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _c.PublishAsync(new Ping(1)).AsTask());
        Assert.Throws<ObjectDisposedException>(() => _c.State<CounterState>());
    }

    [Fact]
    public async Task Disposing_a_scope_disposes_its_descendants()
    {
        _a.Dispose();

        await _root.PublishAsync(new Ping(1), Route.Global);

        Assert.Equal(["Root", "E"], _received);
    }
}
