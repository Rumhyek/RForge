namespace RForge.Blazor.Bus;

/// <summary>Holds one instance of <typeparamref name="TState"/> and notifies subscribers of effective changes.</summary>
public interface IStateStore<TState> where TState : IState<TState>
{
    /// <summary>The current value.</summary>
    TState Value { get; }

    /// <summary>Increments on every effective change.</summary>
    long Version { get; }

    /// <summary>The scope that provides this store.</summary>
    IBusScope Owner { get; }

    /// <summary>Replaces the value. A no-op when <paramref name="value"/> equals <see cref="Value"/>.</summary>
    void Set(TState value);

    /// <summary>Atomically replaces the value with <c>update(Value)</c>. A no-op when the result is equal.</summary>
    void Update(Func<TState, TState> update);

    /// <summary>
    /// Subscribes to effective changes. The callback runs synchronously on the writer's thread, in version order.
    /// Dispose the result to unsubscribe.
    /// </summary>
    IDisposable Subscribe(Action<TState> onChanged);
}
