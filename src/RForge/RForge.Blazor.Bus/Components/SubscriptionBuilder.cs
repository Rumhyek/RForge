using System.Runtime.CompilerServices;

namespace RForge.Blazor.Bus;

/// <summary>
/// Registers a component's event handlers and state handles. Only valid inside
/// <see cref="BusComponentBase.RegisterSubscriptions"/>.
/// </summary>
/// <remarks>
/// Every handler is run through the component's <c>InvokeAsync</c>, followed by <c>StateHasChanged</c> unless
/// <c>render</c> is <see langword="false"/>. Exceptions go to the component's <c>ErrorBoundary</c>.
/// Subscriptions are registered in every render mode. During static rendering and prerendering the renderer keeps
/// re-rendering until async initialization settles, so state written by one component (for example after an await in
/// OnInitializedAsync) still reaches components rendered before it. Those subscriptions end with the request scope.
/// </remarks>
public sealed class SubscriptionBuilder
{
    private readonly BusComponentBase _component;
    private bool _completed;

    internal SubscriptionBuilder(BusComponentBase component) => _component = component;

    /// <summary>Subscribes a synchronous handler.</summary>
    public void On<TEvent>(Action<TEvent> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Add<TEvent>((e, _) =>
        {
            handler(e);
            return ValueTask.CompletedTask;
        }, at, render);
    }

    /// <summary>Subscribes an asynchronous handler.</summary>
    [OverloadResolutionPriority(1)]
    public void On<TEvent>(Func<TEvent, Task> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Add<TEvent>((e, _) => new ValueTask(handler(e)), at, render);
    }

    /// <summary>Subscribes an asynchronous handler.</summary>
    public void On<TEvent>(Func<TEvent, ValueTask> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Add<TEvent>((e, _) => handler(e), at, render);
    }

    /// <summary>Subscribes an asynchronous handler that receives the publish token linked with the component's disposal token.</summary>
    [OverloadResolutionPriority(1)]
    public void On<TEvent>(Func<TEvent, CancellationToken, Task> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Add<TEvent>((e, ct) => new ValueTask(handler(e, ct)), at, render);
    }

    /// <summary>Subscribes an asynchronous handler that receives the publish token linked with the component's disposal token.</summary>
    public void On<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler, ScopeTarget at = default, bool render = true) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Add(handler, at, render);
    }

    /// <summary>Creates a handle to the whole value of <typeparamref name="TState"/>, resolved from the nearest provider.</summary>
    /// <param name="at">Where provider resolution starts, relative to the component's scope.</param>
    /// <param name="render">Whether an effective change re-renders the component.</param>
    public StateHandle<TState> State<TState>(ScopeTarget at = default, bool render = true) where TState : IState<TState>
    {
        EnsureOpen();
        var store = _component.BusScopeInternal.State<TState>(at);
        if (render)
        {
            _component.Track(store.Subscribe(_ => _component.ScheduleRender()));
        }

        return new StateHandle<TState>(store);
    }

    /// <summary>
    /// Creates a handle to a projection of <typeparamref name="TState"/>. The component re-renders only when the
    /// projection changes according to <paramref name="comparer"/> (default equality if null).
    /// </summary>
    public SelectorHandle<TState, TSelected> State<TState, TSelected>(
        Func<TState, TSelected> selector,
        ScopeTarget at = default,
        bool render = true,
        IEqualityComparer<TSelected>? comparer = null)
        where TState : IState<TState>
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureOpen();

        var store = _component.BusScopeInternal.State<TState>(at);
        var handle = new SelectorHandle<TState, TSelected>(store, selector, comparer);
        if (render)
        {
            _component.Track(store.Subscribe(_ => _component.ScheduleRender(handle.Refresh)));
        }

        return handle;
    }

    internal void Complete() => _completed = true;

    private void Add<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler, ScopeTarget at, bool render) where TEvent : IEvent
    {
        EnsureOpen();

        var component = _component;
        var subscription = component.BusScopeInternal.Subscribe<TEvent>((e, ct) => component.DeliverAsync(handler, e, render, ct), at);
        component.Track(subscription);
    }

    private void EnsureOpen()
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                "SubscriptionBuilder can only be used inside RegisterSubscriptions. To subscribe later, use Scope.Subscribe and dispose the result in DisposeAsyncCore.");
        }
    }
}
