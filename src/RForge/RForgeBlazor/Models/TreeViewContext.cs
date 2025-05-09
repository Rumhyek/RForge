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
/// Represents the base context for a tree view for sub components that do not need to know the type of the tree item data.
/// </summary>
public class TreeViewContext
{
    /// <summary>
    /// Gets or sets a value indicating whether selection is allowed.
    /// </summary>
    public bool AllowSelection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether node can be clicked or not.
    /// </summary>
    public bool AllowNodeClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether expansion is allowed.
    /// </summary>
    public bool AllowExpand { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tree view is in prerender mode or not.
    /// </summary>
    public bool ShowAsPrerender { get; set; }

    /// <summary>
    /// Occurs when the selected node changes within the <see cref="RfTreeNode"/>. Changes done outside are not notified.
    /// </summary>
    public event AsyncEventHandler<RfTreeNode> OnSelectedChange;

    /// <summary>
    /// Occurs when the expanded node changes within the <see cref="RfTreeNode"/>. Changes done outside are not notified.
    /// </summary>
    public event AsyncEventHandler<RfTreeNode> OnExpandedChange;

    internal async Task NodeSelectionChange(RfTreeNode rfTreeNode)
    {
        await OnSelectedChange?.Invoke(this, rfTreeNode);
    }

    internal async Task NodeExpandChange(RfTreeNode rfTreeNode)
    {
        await OnExpandedChange?.Invoke(this, rfTreeNode);
    }
}
