using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfTreeList
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string CssClass { get; set; }
}