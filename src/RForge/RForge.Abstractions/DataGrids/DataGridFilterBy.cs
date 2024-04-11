namespace RForge.Abstractions.DataGrids;

/// <summary>
/// The event args of the Data Grid for OnFilterChanged that is raised when a fitler changes.
/// </summary>
public class DataGridFilterBy
{
    /// <summary>
    /// The value of the filter that changed.
    /// </summary>
    public object Value { get; set; }
}
