using Microsoft.AspNetCore.Components;

namespace RForgeBlazor;
public partial class RfDgFilterInputFloat
{

    [Parameter]
    public float? MinValue { get; set; }

    [Parameter]
    public float? MaxValue { get; set; }

    [Parameter]
    public float Step { get; set; } = 1;

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