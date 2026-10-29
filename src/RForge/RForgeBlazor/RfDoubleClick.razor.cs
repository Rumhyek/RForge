using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading;

namespace RForgeBlazor;


/// <summary>
/// A Blazor component that distinguishes between single and double clicks on an element.
/// </summary>
/// <example>
/// <code>
/// <RfDoubleClick OnSingleClick="HandleClick" OnDoubleClick="HandleDoubleClick" Element="button">
///     Click or Double Click Me
/// </RfDoubleClick>
///
/// @code {
///     private void HandleClick() { /* Single click logic */ }
///     private void HandleDoubleClick() { /* Double click logic */ }
/// }
/// </code>
/// </example>
public class RfDoubleClick : ComponentBase, IDisposable
{
    /// <summary>
    /// Callback invoked on a single click.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnSingleClick { get; set; }

    /// <summary>
    /// Callback invoked on a double click.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnDoubleClick { get; set; }

    /// <summary>
    /// The HTML element to render (e.g., "button", "div", "span"). 
    /// Default is "button". Setting this parameter to null, empty, or whitespace
    /// falls back to the default "button" element (the fallback is documented
    /// behavior since #76; previously an empty element name produced a render that
    /// threw on web hosts and rendered no queryable element under test renderers).
    /// </summary>
    [Parameter]
    public string Element { get; set; } = DefaultElement;

    /// <summary>
    /// The delay in milliseconds to distinguish between single and double clicks.
    /// Default is 400ms.
    /// </summary>
    [Parameter]
    public int ClickDelay { get; set; } = 400;

    /// <summary>
    /// The content to be rendered inside the element.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Additional attributes to be applied to the rendered element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> Attributes { get; set; }

    private const string DefaultElement = "button";

    private int currentClickCount = 0;

    /// <summary>
    /// Cancels the pending single-click delay when the component is disposed, so the
    /// <see cref="OnSingleClick"/> callback can never fire after disposal (#76).
    /// </summary>
    private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();

    /// <summary>
    /// Builds the render tree for the component.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> used to build the component's render tree.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        // A null/empty Element falls back to the default element instead of handing
        // OpenElement an invalid name (#76).
        builder.OpenElement(0, string.IsNullOrWhiteSpace(Element) ? DefaultElement : Element);
        builder.AddMultipleAttributes(1, Attributes);

        // Optimize if they don't provide a double click.
        if (OnDoubleClick.HasDelegate == true)
        {
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClickAndDoubleClick));
        }
        else
        {
            builder.AddAttribute(2, "onclick", OnSingleClick);
        }

        builder.AddContent(4, ChildContent);

        builder.CloseElement();
    }

    /// <summary>
    /// Handles click and double click logic, invoking the appropriate callback.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task OnClickAndDoubleClick(MouseEventArgs e)
    {
        currentClickCount++;

        // If this was clicked only once then fire off OnSingleClick.
        if (currentClickCount == 1)
        {
            try
            {
                await Task.Delay(ClickDelay, lifetimeCancellation.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // The component was disposed while the click delay was pending:
                // drop the single click instead of invoking the callback after disposal (#76).
                return;
            }

            // Belt and braces: the delay can also complete in the same tick as disposal,
            // so re-check the lifetime before invoking the parent callback (#76).
            if (lifetimeCancellation.IsCancellationRequested)
            {
                return;
            }

            if (currentClickCount == 1 && OnSingleClick.HasDelegate == true)
            {
                // Reset this back to 0 after this has fired.
                currentClickCount = 0;

                await OnSingleClick.InvokeAsync(e);
            }
            else
            {
                // Reset this back to 0 after this has fired.
                currentClickCount = 0;
            }
        }
        else if (currentClickCount >= 2)
        {
            // Double click
            await OnDoubleClick.InvokeAsync(e);
            currentClickCount = 0;
        }
    }

    /// <summary>
    /// Disposes the component: cancels any pending single-click delay so no callback
    /// is invoked after disposal.
    /// </summary>
    public void Dispose()
    {
        lifetimeCancellation.Cancel();
        lifetimeCancellation.Dispose();
    }
}


