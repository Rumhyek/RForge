using Microsoft.AspNetCore.Components;
using RForgeBlazor.Services;

namespace RForgeBlazor.Models;

/// <summary>
/// Base class for modal components.
/// </summary>
public class RfModalBase : ComponentBase
{
    /// <summary>
    /// Any css classes to add to the base modal div.
    /// </summary>
    [Parameter]
    public string ModalCss { get; set; }

    /// <summary>
    /// Any css classes to add to .modal-content div.
    /// </summary>
    [Parameter]
    public string ModalContentCss { get; set; }

    /// <summary>
    /// An override of the width of the modal. Can be any type of css expression.
    /// </summary>
    [Parameter]
    public string Width { get; set; }

    /// <summary>
    /// If true the modal is shown otherise the modal is hidden. two way binding.
    /// </summary>
    [Parameter]
    public bool IsVisible { get; set; }
    
    /// <summary>
    /// Event callback for when the visibility of the modal changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    /// <summary>
    /// Should the default close button be shown. By default = true
    /// </summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Adds base modal CSS classes.
    /// </summary>
    /// <param name="css">The CSS classes to add.</param>
    protected void addBaseModalCss(ref string css)
    {
        if (IsVisible == false) return;

        CssHelper.AddIfTrue(ref css, IsVisible == true, "is-active");
        CssHelper.AddIfTrue(ref css, string.IsNullOrEmpty(ModalCss) == false, ModalCss);
    }

    /// <summary>
    /// Gets the CSS classes for the modal.
    /// </summary>
    protected virtual string modalCss
    {
        get
        {
            string css = null;

            addBaseModalCss(ref css);

            return css;
        }
    }

    /// <summary>
    /// Adds base modal styles.
    /// </summary>
    /// <param name="styles">The styles to add.</param>
    protected void addBaseModalStyles(ref string styles)
    {
        if (IsVisible == false) return;

        CssHelper.AddIfTrue(ref styles, string.IsNullOrWhiteSpace(Width) == false, () => $"--bulma-modal-content-width: {Width};");
    }

    /// <summary>
    /// Gets the styles for the modal.
    /// </summary>
    protected virtual string modalStyles
    {
        get
        {
            string styles = null;

            addBaseModalStyles(ref styles);

            return styles;
        }
    }

    /// <summary>
    /// Adds base modal content CSS classes.
    /// </summary>
    /// <param name="css">The CSS classes to add.</param>
    protected void addBaseModalContentCss(ref string css)
    {
        if (IsVisible == false) return;

        CssHelper.AddIfTrue(ref css, string.IsNullOrEmpty(ModalContentCss) == false, ModalContentCss);
    }

    /// <summary>
    /// Gets the CSS classes for the modal content.
    /// </summary>
    protected virtual string modalContentCss
    {
        get
        {
            string css = null;

            addBaseModalContentCss(ref css);

            return css;
        }
    }


}
