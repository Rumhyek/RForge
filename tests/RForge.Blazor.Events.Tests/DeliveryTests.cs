namespace RForge.Blazor.Events.Tests;

public sealed class DeliveryTests
{
    private static ValueTask Log(List<string> log, string entry)
    {
        log.Add(entry);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Handlers_run_sequentially_in_subscription_order()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var log = new List<string>();

        root.Subscribe<Ping>(async (_, _) =>
        {
            await Task.Delay(20);
            log.Add("first");
        });
        root.Subscribe<Ping>((_, _) => Log(log, "second"));

        await root.PublishAsync(new Ping(1));

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task Failing_handler_is_logged_and_does_not_stop_delivery_by_default()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var log = new List<string>();

        root.Subscribe<Ping>((_, _) => throw new InvalidOperationException("nope"));
        root.Subscribe<Ping>((_, _) => Log(log, "after"));

        await root.PublishAsync(new Ping(1));

        Assert.Equal(["after"], log);
    }

    [Fact]
    public async Task Throw_mode_runs_all_handlers_then_aggregates()
    {
        using var root = new RootEventHub(new RfEventOptions { HandlerErrorMode = HandlerErrorMode.Throw });
        var log = new List<string>();

        root.Subscribe<Ping>((_, _) => throw new InvalidOperationException("one"));
        root.Subscribe<Ping>((_, _) => Log(log, "middle"));
        root.Subscribe<Ping>((_, _) => throw new InvalidOperationException("two"));

        var error = await Assert.ThrowsAsync<AggregateException>(() => root.PublishAsync(new Ping(1)).AsTask());

        Assert.Equal(["middle"], log);
        Assert.Equal(["one", "two"], error.InnerExceptions.Select(e => e.Message));
    }

    [Fact]
    public async Task Nested_publishes_run_inline()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var log = new List<string>();

        root.Subscribe<Ping>(async (e, ct) =>
        {
            log.Add("ping:start");
            await root.PublishAsync(new Pong("nested"), cancellationToken: ct);
            log.Add("ping:end");
        });
        root.Subscribe<Ping>((_, _) => Log(log, "ping:second"));
        root.Subscribe<Pong>((_, _) => Log(log, "pong"));

        await root.PublishAsync(new Ping(1));

        Assert.Equal(["ping:start", "pong", "ping:end", "ping:second"], log);
    }

    [Fact]
    public async Task Event_loops_are_stopped_at_max_depth()
    {
        using var root = new RootEventHub(new RfEventOptions { MaxPublishDepth = 4, HandlerErrorMode = HandlerErrorMode.Throw });
        var depth = 0;

        root.Subscribe<Ping>(async (e, ct) =>
        {
            depth++;
            await root.PublishAsync(new Ping(e.Value + 1), cancellationToken: ct);
        });

        var error = await Assert.ThrowsAsync<AggregateException>(() => root.PublishAsync(new Ping(0)).AsTask());

        Assert.Equal(4, depth);
        Assert.Contains(Flatten(error), e => e is RfEventReentrancyException);
    }

    [Fact]
    public async Task Cancellation_stops_remaining_handlers_and_surfaces()
    {
        using var root = new RootEventHub(new RfEventOptions());
        using var cts = new CancellationTokenSource();
        var log = new List<string>();

        root.Subscribe<Ping>((_, _) =>
        {
            cts.Cancel();
            return Log(log, "first");
        });
        root.Subscribe<Ping>((_, _) => Log(log, "second"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root.PublishAsync(new Ping(1), cancellationToken: cts.Token).AsTask());
        Assert.Equal(["first"], log);
    }

    [Fact]
    public async Task Receivers_are_snapshotted_when_publish_starts()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var log = new List<string>();
        IDisposable? second = null;

        root.Subscribe<Ping>((_, _) =>
        {
            second!.Dispose(); // unsubscribed before it runs: skipped
            root.Subscribe<Ping>((_, _) => Log(log, "late")); // added during delivery: not called
            return Log(log, "first");
        });
        second = root.Subscribe<Ping>((_, _) => Log(log, "second"));

        await root.PublishAsync(new Ping(1));
        Assert.Equal(["first"], log);
    }

    [Fact]
    public async Task Disposed_subscription_no_longer_receives()
    {
        using var root = new RootEventHub(new RfEventOptions());
        var count = 0;
        var subscription = root.Subscribe<Ping>((_, _) =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        await root.PublishAsync(new Ping(1));
        subscription.Dispose();
        subscription.Dispose(); // idempotent
        await root.PublishAsync(new Ping(2));

        Assert.Equal(1, count);
    }

    private static IEnumerable<Exception> Flatten(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions.SelectMany(Flatten))
            {
                yield return inner;
            }
        }
        else if (exception.InnerException is { } inner)
        {
            foreach (var nested in Flatten(inner))
            {
                yield return nested;
            }
        }
    }
}
