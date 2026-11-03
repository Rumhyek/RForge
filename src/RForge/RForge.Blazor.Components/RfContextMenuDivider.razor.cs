using Microsoft.AspNetCore.Components;

namespace RForge.Blazor;
/// <summary>
/// Represents a divider in a context menu.
/// </summary>
public partial class RfContextMenuDivider
{
    /// <summary>
    /// Gets or sets the CSS class for the divider.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }
}
