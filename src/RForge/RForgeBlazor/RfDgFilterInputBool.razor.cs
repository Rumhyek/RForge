using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;
public partial class RfDgFilterInputBool : RfDgFilterSelect<bool?>
{
    public override string DefaultAriaLabelValue => "True / False Filter";

    #region Parameters

    [Parameter]
    public string TrueTextValue { get; set; } = true.ToString();
    [Parameter]
    public string FalseTextValue { get; set; } = false.ToString();
    [Parameter]
    public string NullTextValue { get; set; }

    #endregion

    protected override void OnInitialized()
    {
        if (Options == null)
        {
            Options = new List<RfDgFilterOption<bool?>>()
            {
                new RfDgFilterOption<bool?> { Value = null, Text = NullTextValue },
                new RfDgFilterOption<bool?> { Value = true, Text = TrueTextValue },
                new RfDgFilterOption<bool?> { Value = false, Text = FalseTextValue },
            };
        }
    }

    public override async Task OnChange(ChangeEventArgs args)
    {
        if (args.Value == null)
            Value = null;
        else if (bool.TryParse(args.Value.ToString(), out var val))
            Value = val;
        else
            Value = null;

        await NotifyChange(Value);
    }

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputText)} must be within a {nameof(GridContext)}");
    }
}