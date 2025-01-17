using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;

/// <summary>
/// A component for filtering data grid columns by DateTime values.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputDateTime MinValue="new DateTime(2023, 1, 1)" MaxValue="new DateTime(2023, 12, 31)" /&gt;
/// </code>
/// </summary>
public partial class RfDgFilterInputDateTime
{

    #region Parameters
    /// <summary>
    /// The minimum value possible.
    /// </summary>
    [Parameter]
    public DateTime? MinValue { get; set; }

    /// <summary>
    /// The maximum value possible.
    /// </summary>
    [Parameter]
    public DateTime? MaxValue { get; set; }
    #endregion

    /// <summary>
    /// Gets the default ARIA label value.
    /// </summary>
    public override string DefaultAriaLabelValue => "DateTime Filter";

    /// <summary>
    /// Handles the change event of the input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else if (DateTime.TryParse(args.Value.ToString(), out var val))
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
            throw new ArgumentNullException($"{nameof(RfDgFilterInputDateTime)} must be within a {nameof(GridContext)}");
    }
}