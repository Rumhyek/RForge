using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// A component for filtering data grid columns by boolean values.
/// Example usage:
/// <code>
/// &lt;RfDgFilterInputBool TrueTextValue="Yes" FalseTextValue="No" NullTextValue="Any" /&gt;
/// </code>
/// </summary>
public partial class RfDgFilterInputBool : RfDgFilterSelect<bool?>
{
    /// <summary>
    /// Gets the default ARIA label value.
    /// </summary>
    public override string DefaultAriaLabelValue => "True / False Filter";

    #region Parameters

    /// <summary>
    /// The text to display for the true value.
    /// </summary>
    [Parameter]
    public string TrueTextValue { get; set; } = true.ToString();

    /// <summary>
    /// The text to display for the false value.
    /// </summary>
    [Parameter]
    public string FalseTextValue { get; set; } = false.ToString();

    /// <summary>
    /// The text to display for the null value.
    /// </summary>
    [Parameter]
    public string NullTextValue { get; set; }

    #endregion

    /// <summary>
    /// Initializes the component. Sets the default options if none are provided.
    /// </summary>
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

    /// <summary>
    /// Handles the change event of the input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Sets the parameters for the component.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the component is not within a GridContext.</exception>
    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgFilterInputText)} must be within a {nameof(GridContext)}");
    }
}