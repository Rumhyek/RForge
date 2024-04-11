namespace RForge.Abstractions;

/// <summary>
/// How to sort a column in the Data Grid
/// </summary>
public enum RfSortOrder
{
    /// <summary>
    /// Used to state: not currently sorted.
    /// </summary>
    None,
    /// <summary>
    /// Sort the column in ascending order.
    /// </summary>
    Ascending,
    /// <summary>
    /// Sort the column in descending order.
    /// </summary>
    Descending
}
