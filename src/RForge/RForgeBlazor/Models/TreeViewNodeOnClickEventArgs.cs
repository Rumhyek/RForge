namespace RForgeBlazor.Models;

/// <summary>
/// Provides data for the <see cref="RfTreeNode.NodeClick" /> event callback.
/// </summary>
public class TreeViewNodeOnClickEventArgs
{
    /// <summary>
    /// The reference to the instance of <see cref="RfTreeNode" />.
    /// </summary>
    public RfTreeNode NodeReference { get; internal set; }

}