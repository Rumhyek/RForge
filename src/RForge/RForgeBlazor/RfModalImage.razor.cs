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
    [Parameter]
    ///<summary>
    /// Any classes to apply to the wrapping div 
    ///</summary>
    public string ImageCss { get; set; }

    [Parameter]
    ///<summary>
    /// If set the image will set the aspect ratio given.
    ///</summary>
    public AspectRatio? AspectRatio { get; set; }

    [Parameter]
    ///<summary>
    /// Text to add to the image title and below the image.
    ///</summary>
    public string ImageCaption { get; set; }


    [Parameter]
    ///<summary>
    /// The url use to show the image.
    ///</summary>
    public string ImageUrl { get; set; }
    #endregion

    private async Task OnCloseClick()
    {
        await IsVisibleChanged.InvokeAsync(false);
    }

    #region Computeds
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