using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Models;

/// <summary>
/// Provides data for the <see cref="RfTreeNode{TTreeItemData}.NodeExpandChange" /> event callback.
/// </summary>
/// <typeparam name="TTreeItemData">The type of the tree item data.</typeparam>
public class TreeViewNodeIsExpandEventArgs<TTreeItemData> where TTreeItemData : class
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
    /// A value indicating whether the node is expanded.
    /// </summary>
    public bool IsExpanded { get; internal set; }
}
