using Microsoft.Extensions.Logging;

namespace RForge.Blazor.Events;

internal sealed class StateStore<TState> : IStateStore<TState>, IDisposable where TState : IState<TState>
{
    // _gate guards the value; _notifyGate serializes write + notify so callbacks observe changes in version order.
    // _notifyGate is a Monitor (re-entrant), so a callback may write to the same store.
    private readonly object _gate = new();
    private readonly object _notifyGate = new();
    private readonly EventHubScope _owner;
    private TState _value;
    private long _version;
    private Subscription[] _subscriptions = [];
    private bool _disposed;

    public StateStore(EventHubScope owner, TState initial)
    {
        _owner = owner;
        _value = initial;
    }

    public TState Value
    {
        get { lock (_gate) return _value; }
    }

    public long Version
    {
        get { lock (_gate) return _version; }
    }

    public IRfEventHubScope Owner => _owner;

    public void Set(TState value) => Update(_ => value);

    public void Update(Func<TState, TState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (_notifyGate)
        {
            TState next;
            Subscription[] subscriptions;

            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                var current = _value;
                next = update(current);
                if (EqualityComparer<TState>.Default.Equals(current, next))
                {
                    return;
                }

                _value = next;
                _version++;
                subscriptions = _subscriptions;
            }

            foreach (var subscription in subscriptions)
            {
                if (subscription.IsDisposed)
                {
                    continue;
                }

                try
                {
                    subscription.Callback(next);
                }
                catch (Exception ex)
                {
                    _owner.Root.Logger.LogError(ex, "A change callback for state {StateType} threw an exception.", typeof(TState).FullName);
                }
            }
        }
    }

    public IDisposable Subscribe(Action<TState> onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);

        var subscription = new Subscription(this, onChanged);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _subscriptions = [.. _subscriptions, subscription];
        }

        return subscription;
    }

    private void Unsubscribe(Subscription subscription)
    {
        lock (_gate)
        {
            var index = Array.IndexOf(_subscriptions, subscription);
            if (index >= 0)
            {
                _subscriptions = [.. _subscriptions[..index], .. _subscriptions[(index + 1)..]];
            }
        }
    }

    public void Dispose()
    {
        Subscription[] subscriptions;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            subscriptions = _subscriptions;
            _subscriptions = [];
        }

        foreach (var subscription in subscriptions)
        {
            subscription.MarkDisposed();
        }
    }

    private sealed class Subscription(StateStore<TState> store, Action<TState> callback) : IDisposable
    {
        private int _disposed;

        public Action<TState> Callback { get; } = callback;

        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        public void MarkDisposed() => Interlocked.Exchange(ref _disposed, 1);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                store.Unsubscribe(this);
            }
        }
    }
}
