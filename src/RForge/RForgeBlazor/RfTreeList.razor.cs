using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
/// <summary>
/// Represents a root node in a tree used within <see cref="RfTreeView"/> component.
/// </summary>
public partial class RfTreeList
{
    /// <summary>
    /// Gets or sets the child content to be rendered inside the tree list. Should use <see cref="RfTreeNode"/> components.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the CSS class to be applied to the tree list. Added besides the .menu-list class.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }
}
