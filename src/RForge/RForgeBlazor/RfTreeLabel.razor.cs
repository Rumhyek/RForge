using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfTreeLabel
{
    [Parameter]
    public string CssClass { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }
}