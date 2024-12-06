using Markdig;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace RForgeBlazor;

/// <summary>
/// A Markdown previewer making use of <see cref="Markdig.Markdown"/> as the parser.
/// </summary>
/// <example>
/// <code>
/// </code>
/// </example>
public partial class RfMarkdown : ComponentBase
{
    /// <summary>
    /// Gets or sets the Css class to add to this mark down previewer. Refer to: https://bulma.io/documentation/elements/content/
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Markdown string to render as html. 
    /// </summary>
    [Parameter]
    public string Markdown { get; set; }

    /// <summary>
    /// Allows for customization for Markdown processor via <see cref="Markdig.MarkdownPipeline" href="https://github.com/xoofx/markdig" />
    /// </summary>
    [Parameter]
    public MarkdownPipeline Pipeline { get; set; }

    private bool _isRenderComplete = false;
    private string html;

    protected override void OnInitialized()
    {
        ParseMarkdown();
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        _isRenderComplete = true;
    }

    protected override void OnParametersSet()
    {
        if (_isRenderComplete == false) return;
        
        ParseMarkdown();
    }

    private void ParseMarkdown()
    {
        if (string.IsNullOrEmpty(Markdown) == false)
        {
            if (Pipeline == null)
                html = Markdig.Markdown.ToHtml(Markdown);
            else
                html = Markdig.Markdown.ToHtml(Markdown, Pipeline);
        }
        else
        {
            html = null;
        }
    }
}