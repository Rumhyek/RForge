namespace RForge.Abstractions.DataGrids;

/// <summary>
/// Event args used when a data grid fires OnSortChanged
/// </summary>
public class DataGridSortBy
{
    /// <summary>
    /// The sort key of the header requesting to be sorted by.
    /// </summary>
    public string SortKey { get; set; }
    /// <summary>
    /// The direction to sort by.
    /// </summary>
    public RfSortOrder SortOrder { get; set; }
}
