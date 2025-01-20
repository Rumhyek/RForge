using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForgeBlazor.Models;
using RForgeBlazor.Services;

namespace RForgeBlazor;

/// <summary>
/// Modal component designed to simplify showing an image.
/// </summary>
/// <example>
/// Simple example showcasing 
/// <code>
/// &lt;RfModalImage ImageUrl="https://placehold.co/600x600/" IsVisible="true" /&gt;
/// </code>
/// </example>
public partial class RfModalImage : RfModalBase
{

    #region Parameters
    ///<summary>
    /// Any classes to apply to the wrapping div 
    ///</summary>
    [Parameter]
    public string ImageCss { get; set; }

    ///<summary>
    /// If set the image will set the aspect ratio given.
    ///</summary>
    [Parameter]
    public AspectRatio? AspectRatio { get; set; }

    ///<summary>
    /// Text to add to the image title and below the image.
    ///</summary>
    [Parameter]
    public string ImageCaption { get; set; }


    ///<summary>
    /// The url use to show the image.
    ///</summary>
    [Parameter]
    public string ImageUrl { get; set; }
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
    /// Gets the styles for the image container.
    /// </summary>
    private string imageContainerStyles
    {
        get
        {
            if (IsVisible == false) return null;

            string styles = null;
            CssHelper.AddIfTrue(ref styles, AspectRatio != null, () => $"aspect-ratio: {AspectRatio.Value.Width} / {AspectRatio.Value.Height};");

            return styles;
        }
    }

    /// <summary>
    /// Gets the CSS classes for the image container.
    /// </summary>
    private string imageContainerCss
    {
        get
        {
            if (IsVisible == false) return null;

            string css = null;
            CssHelper.AddIfTrue(ref css, AspectRatio != null, "custom-ratio");
            CssHelper.AddIfTrue(ref css, string.IsNullOrWhiteSpace(ImageCss) == false, ImageCss);
            return css;
        }
    }

    #endregion
}