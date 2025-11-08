namespace RForgeBlazor.Models;

/// <summary>
/// Provides data for the <see cref="RfTreeNode.NodeSelectChange" /> event callback.
/// </summary>
public class TreeViewNodeIsSelectedEventArgs
{
    /// <summary>
    /// The reference to the instance of <see cref="RfTreeNode" />.
    /// </summary>
    public RfTreeNode NodeReference { get; internal set; }

    /// <summary>
    /// A value indicating whether the node is selected.
    /// </summary>
    public bool IsSelected { get; internal set; }
}
