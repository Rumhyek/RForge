using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Models;


/// <summary>
/// Represents an synchronous event handler.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event arguments.</param>
/// <returns>A task representing the synchronous operation.</returns>
public delegate void RfEventHandler<TEventArgs>(object sender, TEventArgs e);

/// <summary>
/// Represents an synchronous event handler that returns a boolean value.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event arguments.</param>
/// <returns>A task representing the synchronous operation that returns a boolean value.</returns>
public delegate bool RfBoolEventHandler<TEventArgs>(object sender, TEventArgs e);


public class TreeViewContext<TTreeItemData> where TTreeItemData : class
{
    public bool AllowSelection { get; set; }
    public bool AllowExpand { get; set; }

    public event RfEventHandler<RfTreeNode<TTreeItemData>> OnSelectedChange;

    public event RfEventHandler<RfTreeNode<TTreeItemData>> OnExpandedChange;

    internal void NodeSelectionChange(RfTreeNode<TTreeItemData> rfTreeNode)
    {
        OnSelectedChange?.Invoke(this, rfTreeNode);
    }

    internal void NodeExpandChange(RfTreeNode<TTreeItemData> rfTreeNode)
    {
        OnExpandedChange?.Invoke(this, rfTreeNode);
    }
}
