using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace RForge.Blazor.Bus;

/// <summary>
/// Creates a child scope that provides its own instance of <typeparamref name="TState"/> to its subtree.
/// Components below it resolve <typeparamref name="TState"/> to this instance instead of the root's.
/// </summary>
/// <remarks>
/// A <see cref="StateScope{TState}"/> is a full scope: it is the nearest scope of the components inside it, so
/// <see cref="Route.Local"/> from inside reaches only this scope's subscribers.
/// </remarks>
public sealed class StateScope<TState> : ComponentBase, IDisposable where TState : IState<TState>
{
    private BusScope? _scope;
    private StateStore<TState>? _store;
    private IDisposable? _cascadeSubscription;
    private bool _cascadeValue;
    private RenderFragment? _cascadedContent;

    [CascadingParameter]
    private IBusScope? ParentScope { get; set; }

    [Inject]
    private IBus RootBus { get; set; } = default!;

    /// <summary>Optional name for <see cref="ScopeTarget.Named"/> and <see cref="Route.Named"/>. Read once at initialization.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Starting value. Defaults to <c>TState.Initial</c>. Read once at initialization; later changes are ignored.</summary>
    [Parameter]
    public TState? Initial { get; set; }

    /// <summary>
    /// When <see langword="true"/>, also cascades the current value (non-fixed) so plain components can read it with
    /// <c>[CascadingParameter]</c>. Those consumers re-render on every effective change. Read once at initialization.
    /// </summary>
    [Parameter]
    public bool CascadeValue { get; set; }

    /// <summary>The subtree that sees this scope.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (_scope is not null && parameters.TryGetValue<TState>(nameof(Initial), out var initial)
            && !EqualityComparer<TState>.Default.Equals(initial, Initial))
        {
            ((RootBus)_scope.Root).Logger.LogWarning(
                "StateScope<{StateType}>.Initial changed after initialization; the change is ignored. Write to the store instead.",
                typeof(TState).Name);
        }

        if (_scope is null)
        {
            parameters.SetParameterProperties(this);
            var hasInitial = parameters.TryGetValue<TState>(nameof(Initial), out _);
            Initialize(hasInitial);
        }

        return base.SetParametersAsync(parameters);
    }

    private void Initialize(bool hasInitial)
    {
        _scope = BusScope.From(ParentScope ?? RootBus).CreateChild(Name);
        _store = _scope.Provide(hasInitial && Initial is not null ? Initial : TState.Initial);
        _cascadeValue = CascadeValue;

        if (_cascadeValue)
        {
            _cascadedContent = RenderCascadedValue;
            _cascadeSubscription = _store.Subscribe(_ => InvokeAsync(StateHasChanged).Observe());
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        ScopeRendering.RenderFixedScope(builder, _scope!, _cascadeValue ? _cascadedContent : ChildContent);

    private void RenderCascadedValue(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<TState>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<TState>.Value), _store!.Value);
        builder.AddComponentParameter(2, nameof(CascadingValue<TState>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cascadeSubscription?.Dispose();
        _scope?.Dispose();
    }
}
