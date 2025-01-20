using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;

/// <summary>
/// A component for filtering data grid columns by date values.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputDate MinValue="new DateTime(2023, 1, 1)" MaxValue="new DateTime(2023, 12, 31)" /&gt;
/// </code>
/// </summary>
public partial class RfDgFilterInputDate
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
    /// Gets the default ARIA label value for the date filter.
    /// </summary>
    public override string DefaultAriaLabelValue => "Date Filter";

    /// <summary>
    /// Handles the change event for the date input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else if (DateTime.TryParse(args.Value.ToString(), out var val))
            Value = val.Date;
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
    /// Called when the component's parameters are set.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the component is not within a GridContext.</exception>
    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputDate)} must be within a {nameof(GridContext)}");
    }
}