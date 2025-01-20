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
    /// <summary>
    /// Content to be rendered inside the modal.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private async Task OnCloseClick()
    {
        await IsVisibleChanged.InvokeAsync(false);
    }
}