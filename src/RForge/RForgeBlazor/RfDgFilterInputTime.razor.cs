using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputTime
{
    #region Parameters
    [Parameter]
    /// <summary>
    /// The minimum allowed time.
    /// </summary>
    public TimeOnly? MinValue { get; set; }

    [Parameter]
    /// <summary>
    /// The maximum allowed time.
    /// </summary>
    public TimeOnly? MaxValue { get; set; } 
    #endregion

    public override string DefaultAriaLabelValue => "Time Filter";


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

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputTime)} must be within a {nameof(GridContext)}");
    }
}