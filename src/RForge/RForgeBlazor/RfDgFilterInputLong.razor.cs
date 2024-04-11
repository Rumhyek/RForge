using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputLong
{

    [Parameter]
    public long? MinValue { get; set; }

    [Parameter]
    public long? MaxValue { get; set; }

    [Parameter]
    public long Step { get; set; } = 1;

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