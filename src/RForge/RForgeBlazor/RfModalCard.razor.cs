using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
/// <summary>
/// Represents a modal card component.
/// </summary>
/// <example>
/// <code>
/// &lt;RfModalCard Title="Example Modal" IsVisible="true"&gt;
///     &lt;Body&gt;
///         &lt;p&gt;This is the body content of the modal.&lt;/p&gt;
///     &lt;/Body&gt;
///     &lt;Foot&gt;
///         &lt;button class="button" @onclick="CloseModal"&gt;Close&lt;/button&gt;
///     &lt;/Foot&gt;
/// &lt;/RfModalCard&gt;
/// </code>
/// </example>
public partial class RfModalCard
{

    #region Parameters
    ///<summary>
    /// Content to put within the header of the card modal.
    ///</summary>
    [Parameter]
    public RenderFragment Head { get; set; }
    
    ///<summary>
    /// Sets the title of the card modal. Will be ignored if <see cref="Head"/> is used.
    ///</summary>
    [Parameter]
    public string Title { get; set; }

    ///<summary>
    /// Content to put within the body of the card modal.
    ///</summary>
    [Parameter]
    public RenderFragment Body { get; set; }

    ///<summary>
    /// Content to put within the footer of the card modal.
    ///</summary>
    [Parameter]
    public RenderFragment Foot { get; set; }
    #endregion

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private async Task OnCloseClick()
    {
        await IsVisibleChanged.InvokeAsync(false);
    }

    #region Computeds
    /// <summary>
    /// Determines whether the header should be shown.
    /// </summary>
    private bool showHead
    {
        get
        {
            return Head != null || string.IsNullOrWhiteSpace(Title) == false;
        }
    }
    #endregion

}