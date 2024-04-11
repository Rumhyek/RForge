using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputDate
{

    [Parameter]
    public DateTime? MinValue { get; set; }

    [Parameter]
    public DateTime? MaxValue { get; set; }

    public override string DefaultAriaLabelValue => "Date Filter";


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

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputDate)} must be within a {nameof(GridContext)}");
    }
}