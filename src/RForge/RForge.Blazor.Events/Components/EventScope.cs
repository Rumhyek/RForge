using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Events;

/// <summary>
/// Creates a child hub scope and cascades it to its subtree. The cascade is fixed: the scope object never changes,
/// so adding a scope costs nothing per descendant.
/// </summary>
/// <remarks>Declare scopes inside an interactive island: cascading values don't cross render-mode boundaries.</remarks>
public sealed class EventScope : ComponentBase, IDisposable
{
    private EventHubScope? _scope;

    [CascadingParameter]
    private IRfEventHubScope? ParentScope { get; set; }

    [Inject]
    private IRfEventHub RootEventHub { get; set; } = default!;

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
        _scope = EventHubScope.From(ParentScope ?? RootEventHub).CreateChild(Name);

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        ScopeRendering.RenderFixedScope(builder, _scope!, ChildContent);

    /// <inheritdoc />
    public void Dispose() => _scope?.Dispose();
}

internal static class ScopeRendering
{
    public static void RenderFixedScope(RenderTreeBuilder builder, IRfEventHubScope scope, RenderFragment? childContent)
    {
        builder.OpenComponent<CascadingValue<IRfEventHubScope>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<IRfEventHubScope>.Value), scope);
        builder.AddComponentParameter(2, nameof(CascadingValue<IRfEventHubScope>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<IRfEventHubScope>.ChildContent), childContent);
        builder.CloseComponent();
    }
}
