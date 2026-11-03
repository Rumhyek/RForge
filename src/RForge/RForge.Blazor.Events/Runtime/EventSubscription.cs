namespace RForge.Blazor.Events;

internal abstract class EventSubscription(EventHubScope scope, Type eventType) : IDisposable
{
    private int _disposed;

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>Marks the subscription dead without touching the scope (used when the scope itself is disposed).</summary>
    internal void MarkDisposed() => Interlocked.Exchange(ref _disposed, 1);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            scope.RemoveSubscription(eventType, this);
        }
    }
}

internal sealed class EventSubscription<TEvent>(EventHubScope scope, Func<TEvent, CancellationToken, ValueTask> handler)
    : EventSubscription(scope, typeof(TEvent))
{
    public Func<TEvent, CancellationToken, ValueTask> Handler { get; } = handler;
}
