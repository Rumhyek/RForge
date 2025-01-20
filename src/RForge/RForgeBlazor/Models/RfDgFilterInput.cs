using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DataGrids;

namespace RForgeBlazor.Models;

/// <summary>
/// An abstract base class for filter input components in a data grid.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputInt MinValue="0" MaxValue="100" Step="1" /&gt;
/// </code>
/// </summary>
/// <typeparam name="TType">The type of the filter input value.</typeparam>
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
    /// <summary>
    /// The event callback that is invoked when the value of the input changes.
    /// </summary>
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

    /// <summary>
    /// Gets the ARIA label value for the filter. If no title or placeholder is set, it will default to the name.
    /// </summary>
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

    /// <summary>
    /// Gets the default ARIA label value.
    /// </summary>
    public abstract string DefaultAriaLabelValue { get; }

    /// <summary>
    /// Handles the change event of the input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task OnChange(ChangeEventArgs args);

    /// <summary>
    /// Notifies the grid context of a value change.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task NotifyChange(TType value)
    {
        //notify the filter holder first then the gridview
        Value = value;
        await ValueChanged.InvokeAsync(value);

        await GridContext.FilterChanged(new DataGridFilterBy()
        {
            Value = value
        });
    }
}
