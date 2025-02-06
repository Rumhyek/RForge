namespace RForgeBlazor.Models;

/// <summary>
/// Provides data for the <see cref="RfTreeNode{TTreeItemData}.NodeSelectChange" /> event callback.
/// </summary>
/// <typeparam name="TTreeItemData">The type of the tree item data.</typeparam>
public class TreeViewNodeIsSelectedEventArgs<TTreeItemData> where TTreeItemData : class
{
    /// <summary>
    /// The reference to the instance of <see cref="RfTreeNode{TTreeItemData}" />.
    /// </summary>
    public RfTreeNode<TTreeItemData> NodeReference { get; internal set; }

    /// <summary>
    /// The current data of the tree node. <see cref="RfTreeNode{TTreeItemData}.NodeData"/>
    /// </summary>
    public TTreeItemData NodeData { get; internal set; }

    /// <summary>
    /// A value indicating whether the node is selected.
    /// </summary>
    public bool IsSelected { get; internal set; }
}
