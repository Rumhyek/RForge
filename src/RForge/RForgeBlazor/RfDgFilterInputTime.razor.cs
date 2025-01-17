using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;

/// <summary>
/// A component for filtering data grid columns by time.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputTime MinValue="new TimeOnly(8, 0)" MaxValue="new TimeOnly(17, 0)" /&gt;
/// </code>
/// </summary>
public partial class RfDgFilterInputTime
{
    #region Parameters
    /// <summary>
    /// The minimum allowed time.
    /// </summary>
    [Parameter]
    public TimeOnly? MinValue { get; set; }

    /// <summary>
    /// The maximum allowed time.
    /// </summary>
    [Parameter]
    public TimeOnly? MaxValue { get; set; }
    #endregion

    /// <summary>
    /// Gets the default ARIA label value.
    /// </summary>
    public override string DefaultAriaLabelValue => "Time Filter";

    /// <summary>
    /// Handles the change event of the input. This will update the value and notify the parent.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else if (TimeOnly.TryParse(args.Value.ToString(), out var val))
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
    /// Sets the parameters for the component. Required to ensure the component is within a <see cref="RForgeBlazor.Models.DataGridContext"/>.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputTime)} must be within a {nameof(GridContext)}");
    }
}