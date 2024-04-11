using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputFloat
{

    #region Parameters
    [Parameter]
    /// <summary>
    /// The minimum value possible.
    /// </summary>
    public float? MinValue { get; set; }

    [Parameter]
    /// <summary>
    /// The maximum value possible.
    /// </summary>
    public float? MaxValue { get; set; }

    [Parameter]
    /// <summary>
    /// When using the input controls the step it takes.
    /// </summary>
    public float Step { get; set; } = 1;
    #endregion

    public override string DefaultAriaLabelValue => "Number Filter";


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

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputFloat)} must be within a {nameof(GridContext)}");
    }
}