namespace RForgeBlazor.Models;

/// <summary>
/// Represents the base context for a tree view for sub components that do not need to know the type of the tree item data.
/// </summary>
public class TreeViewBaseContext
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
}
