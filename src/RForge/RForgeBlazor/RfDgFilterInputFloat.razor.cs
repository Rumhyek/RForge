using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;

/// <summary>
/// A component for filtering data grid columns by float values.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputFloat MinValue="0.0f" MaxValue="100.0f" Step="0.1f" /&gt;
/// </code>
/// </summary>
public partial class RfDgFilterInputFloat
{

    #region Parameters
    /// <summary>
    /// The minimum value possible.
    /// </summary>
    [Parameter]
    public float? MinValue { get; set; }

    /// <summary>
    /// The maximum value possible.
    /// </summary>
    [Parameter]
    public float? MaxValue { get; set; }

    /// <summary>
    /// When using the input controls the step it takes.
    /// </summary>
    [Parameter]
    public float Step { get; set; } = 1;
    #endregion

    /// <summary>
    /// Gets the default ARIA label value.
    /// </summary>
    public override string DefaultAriaLabelValue => "Number Filter";

    /// <summary>
    /// Handles the change event of the input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else if (float.TryParse(args.Value.ToString(), out float val))
            Value = val;
        else
            Value = null;

        if (Value != null)
        {
            //clamp
            if (MinValue.HasValue == true && MinValue > Value)
                Value = MinValue;

            if (MaxValue.HasValue == true && MaxValue < Value)
                Value = MaxValue;
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
            throw new ArgumentNullException($"{nameof(RfDgFilterInputFloat)} must be within a {nameof(GridContext)}");
    }
}