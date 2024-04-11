using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// A basic shell of a modal component.
/// </summary>
/// <example>
/// <code>
/// &lt;RfModal IsVisible=true &gt;
///     &lt;span$gt;Hello World&lt;/&gt;
/// &lt;/RfModal&gt;
/// </code>
/// </example>
public partial class RfModal : RfModalBase
{

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private async Task OnCloseClick()
    {
        await IsVisibleChanged.InvokeAsync(false);
    }
}