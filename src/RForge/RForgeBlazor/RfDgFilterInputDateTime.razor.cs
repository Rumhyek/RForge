using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputDateTime
{

    #region Parameters
    [Parameter]
    /// <summary>
    /// The minimum value possible.
    /// </summary>
    public DateTime? MinValue { get; set; }

    [Parameter]
    /// <summary>
    /// The maximum value possible.
    /// </summary>
    public DateTime? MaxValue { get; set; }
    #endregion

    public override string DefaultAriaLabelValue => "DateTime Filter";


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

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputDateTime)} must be within a {nameof(GridContext)}");
    }
}