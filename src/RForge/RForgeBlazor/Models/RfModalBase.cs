using Microsoft.AspNetCore.Components;
using RForgeBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Models;
public class RfModalBase : ComponentBase
{

    [Parameter]
    public string ModalCss { get; set; }

    [Parameter]
    public string ModalContentCss { get; set; }

    [Parameter]
    public string Width { get; set; }

    [Parameter]
    public bool IsVisible { get; set; }
    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }


    protected void addBaseModalCss(ref string css)
    {
        if (IsVisible == false) return;

        CssHelper.AddIfTrue(ref css, IsVisible == true, "is-active");
        CssHelper.AddIfTrue(ref css, string.IsNullOrEmpty(ModalCss) == false, ModalCss);
    }
    protected virtual string modalCss
    {
        get
        {
            string css = null;

            addBaseModalCss(ref css);

            return css;
        }
    }

    protected void addBaseModalStyles(ref string styles)
    {
        if (IsVisible == false) return;

        CssHelper.AddIfTrue(ref styles, string.IsNullOrWhiteSpace(Width) == false, () => $"--bulma-modal-content-width: {Width};");
    }
    protected virtual string modalStyles
    {
        get
        {
            string styles = null;

            addBaseModalStyles(ref styles);

            return styles;
        }
    }

    protected void addBaseModalContentCss(ref string css)
    {
        if (IsVisible == false) return;

        CssHelper.AddIfTrue(ref css, string.IsNullOrEmpty(ModalContentCss) == false, ModalContentCss);
    }
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
