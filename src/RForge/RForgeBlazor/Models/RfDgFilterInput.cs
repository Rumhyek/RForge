using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DataGrids;

namespace RForgeBlazor.Models;
public abstract class RfDgFilterInput<TType> : ComponentBase
{
    [CascadingParameter]
    public DataGridContext GridContext { get; set; }

    [Parameter]
    public TType Value { get; set; }
    [Parameter]
    public EventCallback<TType> ValueChanged { get; set; }

    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public string CssClass { get; set; }

    [Parameter]
    public string Title { get; set; }

    public string AriaLabelValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title) == false)
                return Title;

            if (string.IsNullOrWhiteSpace(Placeholder) == false)
                return Placeholder;

            if (string.IsNullOrWhiteSpace(Name) == false)
                return Name;

            return DefaultAriaLabelValue;
        }
    }

    public abstract string DefaultAriaLabelValue { get; }

    public abstract Task OnChange(ChangeEventArgs args);

    public virtual async Task NotifyChange(TType value)
    {
        //notify the filter holder first then the gridview
        await ValueChanged.InvokeAsync(value);

        await GridContext.FilterChanged(new DataGridFilterBy()
        {
            Value = value
        });
    }
}