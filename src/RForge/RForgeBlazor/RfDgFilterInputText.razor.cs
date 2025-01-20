using Microsoft.AspNetCore.Components;
using RForge.Abstractions;

namespace RForgeBlazor;

/// <summary>
/// A component for filtering data grid columns by text input.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputText TrimValue="RfTrimType.TrimBoth" /&gt;
/// </code>
/// </summary>
public partial class RfDgFilterInputText
{
    /// <summary>
    /// How the filter handles triming of the text inputed. By default <see cref="RfTrimType.TrimBoth"/>
    /// </summary>
    [Parameter]
    public RfTrimType TrimValue { get; set; } = RfTrimType.TrimBoth;

    /// <summary>
    /// Gets the default ARIA label value.
    /// </summary>
    public override string DefaultAriaLabelValue => "Text Filter";

    /// <summary>
    /// Handles the change event of the input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else
            Value = args.Value.ToString();

        if (Value != null)
        {
            switch (TrimValue)
            {
                case RfTrimType.TrimBoth:
                    Value = Value.Trim();
                    break;
                case RfTrimType.TrimEnd:
                    Value = Value.TrimEnd();
                    break;
                case RfTrimType.TrimStart:
                    Value = Value.TrimStart();
                    break;
            }
        }

        await NotifyChange(Value);
    }

    /// <summary>
    /// Sets the parameters for the component.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the component is not within a GridContext.</exception>
    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputText)} must be within a {nameof(GridContext)}");
    }
}