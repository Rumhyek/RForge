namespace RForgeBlazor.Models;

/// <summary>
/// Provides data for the <see cref="RfTreeNode{TTreeItemData}.NodeClick" /> event callback.
/// </summary>
/// <typeparam name="TTreeItemData"></typeparam>
public class TreeViewNodeOnClickEventArgs<TTreeItemData> where TTreeItemData : class
{
    /// <summary>
    /// The reference to the instance of <see cref="RfTreeNode{TTreeItemData}" />.
    /// </summary>
    public RfTreeNode<TTreeItemData> NodeReference { get; internal set; }

    /// <summary>
    /// The current data of the tree node. <see cref="RfTreeNode{TTreeItemData}.NodeData"/>
    /// </summary>
    public TTreeItemData NodeData { get; internal set; }
}