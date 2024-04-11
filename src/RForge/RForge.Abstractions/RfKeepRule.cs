namespace RForge.Abstractions;

/// <summary>
/// For components that support multi select with a max selection RfKeepRule tells the component what to do when a user selects
/// an item that causes the max selection to go over./// </summary>
public enum RfKeepRule
{
    /// <summary>
    /// The first item selected will be the first item removed. Acts like a queue structure.
    /// </summary>
    FirstInFirstOut,
    /// <summary>
    /// The last item selected will be the first item removed. Acts like a stack structure.
    /// </summary>
    FirstInLastOut,
    /// <summary>
    /// The component will not remove an item forcing the user to deselect an item first.
    /// </summary>
    ForceDeselection
}