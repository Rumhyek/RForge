namespace RForge.Blazor.Bus;

/// <summary>
/// A component's reactive view of a whole state value. Created by <see cref="SubscriptionBuilder.State{TState}"/>
/// and released automatically when the component is disposed.
/// </summary>
public sealed class StateHandle<TState> where TState : IState<TState>
{
    internal StateHandle(IStateStore<TState> store) => Store = store;

    /// <summary>The store's current value (always read live).</summary>
    public TState Value => Store.Value;

    /// <summary>The underlying store.</summary>
    public IStateStore<TState> Store { get; }

    /// <summary>Forwards to <see cref="IStateStore{TState}.Set"/>.</summary>
    public void Set(TState value) => Store.Set(value);

    /// <summary>Forwards to <see cref="IStateStore{TState}.Update"/>.</summary>
    public void Update(Func<TState, TState> update) => Store.Update(update);
}

/// <summary>
/// A component's reactive view of a projection of a state value. The component re-renders only when the
/// projection changes. Write through <see cref="Store"/>.
/// </summary>
public sealed class SelectorHandle<TState, TSelected> where TState : IState<TState>
{
    private readonly Func<TState, TSelected> _selector;
    private readonly IEqualityComparer<TSelected> _comparer;
    private TSelected _lastSelected;

    internal SelectorHandle(IStateStore<TState> store, Func<TState, TSelected> selector, IEqualityComparer<TSelected>? comparer)
    {
        Store = store;
        _selector = selector;
        _comparer = comparer ?? EqualityComparer<TSelected>.Default;
        _lastSelected = selector(store.Value);
    }

    /// <summary>The selector applied to the store's current value.</summary>
    public TSelected Value => _selector(Store.Value);

    /// <summary>The underlying store.</summary>
    public IStateStore<TState> Store { get; }

    /// <summary>Re-runs the selector; returns whether the projection changed since the last refresh.</summary>
    internal bool Refresh()
    {
        var next = _selector(Store.Value);
        if (_comparer.Equals(_lastSelected, next))
        {
            return false;
        }

        _lastSelected = next;
        return true;
    }
}
