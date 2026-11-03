namespace RForge.Blazor.Events.Tests;

public sealed class StateTests
{
    public sealed class MutableState : IState<MutableState>
    {
        public static MutableState Initial => new();
    }

    [Fact]
    public void Root_creates_state_lazily_from_Initial_and_reuses_it()
    {
        using var root = new RootEventHub(new RfEventOptions());

        var store = root.State<CounterState>();

        Assert.Equal(CounterState.Initial, store.Value);
        Assert.Same(store, root.State<CounterState>());
        Assert.Same(root, store.Owner);
    }

    [Fact]
    public void Equal_values_do_not_notify_or_bump_version()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var store = root.State<CounterState>();
        var notifications = 0;
        store.Subscribe(_ => notifications++);

        store.Set(new CounterState(0, "start")); // equal by record value
        store.Update(s => s with { });

        Assert.Equal(0, notifications);
        Assert.Equal(0, store.Version);

        store.Update(s => s with { Count = 1 });

        Assert.Equal(1, notifications);
        Assert.Equal(1, store.Version);
    }

    [Fact]
    public void Non_record_state_notifies_on_every_new_instance()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var store = root.State<MutableState>();
        var notifications = 0;
        store.Subscribe(_ => notifications++);

        store.Set(new MutableState());
        store.Set(new MutableState());

        Assert.Equal(2, notifications);
    }

    [Fact]
    public void Callbacks_receive_values_in_version_order()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var store = root.State<CounterState>();
        var seen = new List<int>();
        store.Subscribe(s =>
        {
            seen.Add(s.Count);
            if (s.Count == 1)
            {
                store.Update(x => x with { Count = 2 }); // write from inside a callback
            }
        });

        store.Update(s => s with { Count = 1 });

        Assert.Equal([1, 2], seen);
        Assert.Equal(2, store.Value.Count);
    }

    [Fact]
    public async Task Concurrent_updates_are_not_lost()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var store = root.State<CounterState>();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                store.Update(s => s with { Count = s.Count + 1 });
            }
        })));

        Assert.Equal(4000, store.Value.Count);
        Assert.Equal(4000, store.Version);
    }

    [Fact]
    public void A_throwing_callback_does_not_stop_others()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var store = root.State<CounterState>();
        var reached = false;
        store.Subscribe(_ => throw new InvalidOperationException());
        store.Subscribe(_ => reached = true);

        store.Update(s => s with { Count = 1 });

        Assert.True(reached);
    }

    [Fact]
    public void Nearest_provider_wins_and_falls_back_to_root()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var section = root.CreateChild("section");
        var provided = section.Provide(new CounterState(5, "section"));
        var leaf = section.CreateChild(null);
        var sibling = root.CreateChild(null);

        Assert.Same(provided, leaf.State<CounterState>());
        Assert.Same(provided, section.State<CounterState>());
        Assert.Same(root.State<CounterState>(), sibling.State<CounterState>());
        Assert.Same(root.State<CounterState>(), leaf.State<CounterState>(ScopeTarget.Root));
        Assert.NotSame(provided, root.State<CounterState>());
    }

    [Fact]
    public void Disposed_provider_store_rejects_writes()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var section = root.CreateChild(null);
        var store = section.Provide(CounterState.Initial);

        section.Dispose();

        Assert.Throws<ObjectDisposedException>(() => store.Set(new CounterState(1, "x")));
    }

    [Fact]
    public void Unsubscribed_callback_is_not_called()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var store = root.State<CounterState>();
        var calls = 0;
        var subscription = store.Subscribe(_ => calls++);

        subscription.Dispose();
        store.Update(s => s with { Count = 1 });

        Assert.Equal(0, calls);
    }
}
