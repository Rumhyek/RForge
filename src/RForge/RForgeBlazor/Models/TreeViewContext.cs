﻿using System;
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

/// <summary>
/// Represents the context for a tree view.
/// </summary>
/// <typeparam name="TTreeItemData">The type of the tree item data.</typeparam>
public class TreeViewContext<TTreeItemData> : TreeViewBaseContext where TTreeItemData : class
{
   

    /// <summary>
    /// Occurs when the selected node changes within the <see cref="RfTreeNode{TTreeItemData}"/>. Changes done outside are not notified.
    /// </summary>
    public event AsyncEventHandler<RfTreeNode<TTreeItemData>> OnSelectedChange;

    /// <summary>
    /// Occurs when the expanded node changes within the <see cref="RfTreeNode{TTreeItemData}"/>. Changes done outside are not notified.
    /// </summary>
    public event AsyncEventHandler<RfTreeNode<TTreeItemData>> OnExpandedChange;

    internal async Task NodeSelectionChange(RfTreeNode<TTreeItemData> rfTreeNode)
    {
        await OnSelectedChange?.Invoke(this, rfTreeNode);
    }

    internal async Task NodeExpandChange(RfTreeNode<TTreeItemData> rfTreeNode)
    {
        await OnExpandedChange?.Invoke(this, rfTreeNode);
    }
}
