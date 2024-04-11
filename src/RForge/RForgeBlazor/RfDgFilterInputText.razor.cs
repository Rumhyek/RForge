using Microsoft.AspNetCore.Components;
using RForge.Abstractions;

namespace RForgeBlazor;
public partial class RfDgFilterInputText
{
    /// <summary>
    /// How the filter handles triming of the text inputed. By default <see cref="RfTrimType.TrimBoth"/>
    /// </summary>
    [Parameter]
    public RfTrimType TrimValue { get; set; } = RfTrimType.TrimBoth;

    public override string DefaultAriaLabelValue => "Text Filter";

    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else
            Value = args.Value.ToString();

        if (Value != null)
        {
            switch (TrimValue)
            {
                case RfTrimType.TrimBoth:
                    Value = Value.Trim();
                    break;
                case RfTrimType.TrimEnd:
                    Value = Value.TrimEnd();
                    break;
                case RfTrimType.TrimStart:
                    Value = Value.TrimStart();
                    break;
            }
        }

        await NotifyChange(Value);
    }

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputText)} must be within a {nameof(GridContext)}");
    }
}