using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RForgeBlazor.Models;

namespace RForgeBlazor;

public partial class RfTreeView : ComponentBase
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string CssClass { get; set; }

}