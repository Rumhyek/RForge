using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
/// <summary>
/// Represents a context menu link component. Renders a clickable link in a context menu via a.
/// </summary>
public partial class RfContextMenuLink
{
    /// <summary>
    /// Gets or sets the URL of the link.
    /// </summary>
    [Parameter]
    public string Href { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>
    /// Gets or sets the target attribute of the link.
    /// </summary>
    [Parameter]
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the CSS class of the link.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered inside the link.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }
}
