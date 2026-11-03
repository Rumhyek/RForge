using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Bus;

/// <summary>
/// Creates a child bus scope and cascades it to its subtree. The cascade is fixed: the scope object never changes,
/// so adding a scope costs nothing per descendant.
/// </summary>
/// <remarks>Declare scopes inside an interactive island: cascading values don't cross render-mode boundaries.</remarks>
public sealed class EventScope : ComponentBase, IDisposable
{
    private BusScope? _scope;

    [CascadingParameter]
    private IBusScope? ParentScope { get; set; }

    [Inject]
    private IBus RootBus { get; set; } = default!;

    /// <summary>
    /// Optional name for <see cref="ScopeTarget.Named"/> and <see cref="Route.Named"/>. Read once at initialization.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The subtree that sees this scope.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() =>
        _scope = BusScope.From(ParentScope ?? RootBus).CreateChild(Name);

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        ScopeRendering.RenderFixedScope(builder, _scope!, ChildContent);

    /// <inheritdoc />
    public void Dispose() => _scope?.Dispose();
}

internal static class ScopeRendering
{
    public static void RenderFixedScope(RenderTreeBuilder builder, IBusScope scope, RenderFragment? childContent)
    {
        builder.OpenComponent<CascadingValue<IBusScope>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<IBusScope>.Value), scope);
        builder.AddComponentParameter(2, nameof(CascadingValue<IBusScope>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<IBusScope>.ChildContent), childContent);
        builder.CloseComponent();
    }
}
