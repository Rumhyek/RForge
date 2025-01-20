namespace RForgeBlazor.Models;

/// <summary>
/// Represents a filter option for a data grid.
/// </summary>
/// <typeparam name="TType">The type of the filter option value.</typeparam>
public class RfDgFilterOption<TType>
{
    /// <summary>
    /// Gets or sets the text to display for the filter option.
    /// </summary>
    public string Text { get; set; }
    /// <summary>
    /// Gets or sets the value of the filter option.
    /// </summary>
    public TType Value { get; set; }
}