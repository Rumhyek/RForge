using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputLong
{

    #region Parameters
    [Parameter]
    /// <summary>
    /// The minimum value possible.
    /// </summary>
    public long? MinValue { get; set; }

    [Parameter]
    /// <summary>
    /// The maximum value possible.
    /// </summary>
    public long? MaxValue { get; set; }

    [Parameter]
    /// <summary>
    /// When using the input controls the step it takes.
    /// </summary>
    public long Step { get; set; } = 1;
    #endregion

    public override string DefaultAriaLabelValue => "Integer Filter";


    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else if (long.TryParse(args.Value.ToString(), out var val))
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

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputLong)} must be within a {nameof(GridContext)}");
    }
}