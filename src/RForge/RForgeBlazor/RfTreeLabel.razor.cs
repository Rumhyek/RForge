using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
/// <summary>
/// Represents a label in the <see cref="RfTreeView{TTreeItemData}" /> component.
/// </summary>
public partial class RfTreeLabel
{
    /// <summary>
    /// Gets or sets the CSS class for the label. Added besides the .menu-label class.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets the child content to be rendered inside the label.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }
}
