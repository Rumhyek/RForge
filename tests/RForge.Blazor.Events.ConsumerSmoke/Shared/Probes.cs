using RForge.Blazor.Events;

namespace Smoke;

public sealed record Ping(int N) : IEvent;
public sealed record Pong(int N) : IEvent;
public sealed record Pang(int N) : IEvent;

// Attribute API: only works if the source generator loads and runs in the consumer's compiler.
public partial class AttributeProbe : RfEventComponent
{
    [Subscribe] private void OnPing(Ping e) { }
    [Subscribe] private async Task OnPong(Pong e) => await Task.Yield();
    [Subscribe] private ValueTask OnPang(Pang e, CancellationToken ct) => ValueTask.CompletedTask;
}

// Builder API: every handler shape that has to resolve to exactly one overload.
public class BuilderProbe : RfEventComponent
{
    protected override void RegisterSubscriptions(SubscriptionBuilder builder)
    {
        builder.On<Ping>(e => { });
        builder.On<Ping>(e => Task.CompletedTask);
        builder.On<Ping>(e => ValueTask.CompletedTask);
        builder.On<Ping>((e, ct) => Task.CompletedTask);
        builder.On<Ping>((e, ct) => ValueTask.CompletedTask);
        builder.On<Ping>(HandleSync);
        builder.On<Ping>(HandleTask);
        builder.On<Ping>(HandleValueTask);
        builder.On<Ping>(HandleTaskWithToken);
        builder.On<Ping>(HandleValueTaskWithToken);

        // At C# 12 an async lambda has to name its delegate type (the README documents this).
        builder.On<Ping>((Func<Ping, Task>)(async e => await Task.Delay(1)));
        builder.On<Ping>((Func<Ping, CancellationToken, Task>)(async (e, ct) => await Task.Delay(1, ct)));

#if ASYNC_LAMBDAS
        // From C# 13 overload resolution priority picks the Task overload, on every target framework.
        builder.On<Ping>(async e => await Task.Delay(1));
        builder.On<Ping>(async e => { await Task.Delay(1); });
        builder.On<Ping>(async (e, ct) => await Task.Delay(1, ct));
        builder.On<Ping>(async (e, ct) => { await Task.Delay(1, ct); });
#endif
    }

    private void HandleSync(Ping e) { }
    private Task HandleTask(Ping e) => Task.CompletedTask;
    private ValueTask HandleValueTask(Ping e) => ValueTask.CompletedTask;
    private Task HandleTaskWithToken(Ping e, CancellationToken ct) => Task.CompletedTask;
    private ValueTask HandleValueTaskWithToken(Ping e, CancellationToken ct) => ValueTask.CompletedTask;
}
