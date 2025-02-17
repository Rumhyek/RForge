using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
/// <summary>
/// Represents a context menu item component. Renders a non-clickable item in a context menu via div.dropdown-item.
/// </summary>
public partial class RfContextMenuItem
{
    /// <summary>
    /// Gets or sets the CSS class for the context menu item.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets the child content to be rendered inside the context menu item.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }
}
