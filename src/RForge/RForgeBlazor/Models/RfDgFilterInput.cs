using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DataGrids;

namespace RForgeBlazor.Models;
public abstract class RfDgFilterInput<TType> : ComponentBase
{
    #region Parameters
    /// <summary>
    /// The data grid context that allows the filter to communicate with the grid. <see cref="DataGridContext.FilterChanged(DataGridFilterBy)"/>
    /// </summary>
    [CascadingParameter]
    public DataGridContext GridContext { get; set; }

    /// <summary>
    /// The value of the input in the filter. Is twoway binding.
    /// </summary>
    [Parameter]
    public TType Value { get; set; }
    [Parameter]
    public EventCallback<TType> ValueChanged { get; set; }

    /// <summary>
    /// The placeholder text to show in the filter.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; }

    /// <summary>
    /// The name of the value for the input field.
    /// </summary>
    [Parameter]
    public string Name { get; set; }

    /// <summary>
    /// A css class added to the filter.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// A title attribute added to the filter.
    /// </summary>
    [Parameter]
    public string Title { get; set; }
    #endregion

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
