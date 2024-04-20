using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfModalCard
{

    #region Parameters
    [Parameter]
    ///<summary>
    /// Content to put within the header of the card modal.
    ///</summary>
    public RenderFragment Head { get; set; }
    [Parameter]
    ///<summary>
    /// Sets the title of the card modal. Will be ignored if <see cref="Head"/> is used.
    ///</summary>
    public string Title { get; set; }

    [Parameter]
    ///<summary>
    /// Content to put within the body of the card modal.
    ///</summary>
    public RenderFragment Body { get; set; }

    [Parameter]
    ///<summary>
    /// Content to put within the footer of the card modal.
    ///</summary>
    public RenderFragment Foot { get; set; }
    #endregion

    private async Task OnCloseClick()
    {
        await IsVisibleChanged.InvokeAsync(false);
    }

    #region Computeds
    private bool showHead
    {
        get
        {
            return Head != null || string.IsNullOrWhiteSpace(Title) == false;
        }
    }
    #endregion

}