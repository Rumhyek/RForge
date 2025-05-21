using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RForgeBlazor;


/// <summary>
/// A Blazor component that distinguishes between single and double clicks on an element.
/// </summary>
/// <example>
/// <code>
/// <RfDoubleClick OnClick="HandleClick" OnDoubleClick="HandleDoubleClick" Element="button">
///     Click or Double Click Me
/// </RfDoubleClick>
///
/// @code {
///     private void HandleClick() { /* Single click logic */ }
///     private void HandleDoubleClick() { /* Double click logic */ }
/// }
/// </code>
/// </example>
public class RfDoubleClick : ComponentBase
{
    /// <summary>
    /// Callback invoked on a single click.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback invoked on a double click.
    /// </summary>
    [Parameter]
    public EventCallback OnDoubleClick { get; set; }

    /// <summary>
    /// The HTML element to render (e.g., "button", "div", "span"). 
    /// Default is "button".
    /// </summary>
    [Parameter]
    public string Element { get; set; } = "button";

    /// <summary>
    /// The delay in milliseconds to distinguish between single and double clicks.
    /// Default is 250ms.
    /// </summary>
    [Parameter]
    public int ClickDelay { get; set; } = 250;

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

    private int currentClickCount = 0;

    /// <summary>
    /// Builds the render tree for the component.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> used to build the component's render tree.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        builder.OpenElement(0, Element);
        builder.AddMultipleAttributes(1, Attributes);

        // Optimize if they don't provide a double click.
        if (OnDoubleClick.HasDelegate == true)
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, OnClickAndDoubleClick));
        else
            builder.AddAttribute(2, "onclick", OnClick);

        builder.AddContent(4, ChildContent);

        builder.CloseElement();
    }

    /// <summary>
    /// Handles click and double click logic, invoking the appropriate callback.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task OnClickAndDoubleClick()
    {
        currentClickCount++;

        // If this was clicked only once then fire off OnClick.
        if (currentClickCount == 1 && OnClick.HasDelegate == true)
        {
            await Task.Delay(ClickDelay);

            if (currentClickCount == 1)
                await OnClick.InvokeAsync();

            // Reset this back to 0 after this has fired.
            currentClickCount = 0;
        }
        else if (currentClickCount >= 2)
        {
            // Double click
            await OnDoubleClick.InvokeAsync();
            currentClickCount = 0;
        }
    }
}


