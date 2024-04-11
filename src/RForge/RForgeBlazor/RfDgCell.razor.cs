using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgCell<TRowData> : ComponentBase
{

    [Parameter]
    public RenderFragment ChildContent { get; set; }

}