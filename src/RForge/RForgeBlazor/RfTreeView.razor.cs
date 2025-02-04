using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// Represents a tree view component.
/// </summary>
/// <typeparam name="TTreeItemData">The type of the tree item data.</typeparam>
public partial class RfTreeView<TTreeItemData> : ComponentBase, IDisposable where TTreeItemData : class
{
    /// <summary>
    /// Gets or sets the child content to be rendered inside the tree view.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for the tree view.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether selection is allowed.
    /// </summary>
    [Parameter]
    public bool AllowSelection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether expansion is allowed.
    /// </summary>
    [Parameter]
    public bool AllowExpand { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether multiple nodes can be expanded.
    /// </summary>
    [Parameter]
    public bool AllowMultiExpand { get; set; } = true;

    private TreeViewContext<TTreeItemData> Context { get; set; }
    private RfTreeNode<TTreeItemData> SelectedNode { get; set; }
    private RfTreeNode<TTreeItemData> ExpandedNode { get; set; }

    /// <summary>
    /// Method invoked when the component is initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        Context = new TreeViewContext<TTreeItemData>()
        {
            AllowSelection = AllowSelection,
            AllowExpand = AllowExpand,
        };

        Context.OnExpandedChange += Context_OnExpanded;
        Context.OnSelectedChange += Context_OnSelected;
        base.OnInitialized();
    }

    private void Context_OnSelected(object sender, RfTreeNode<TTreeItemData> selectedNode)
    {
        if (SelectedNode != null && selectedNode != SelectedNode)
            SelectedNode.Deselect();

        SelectedNode = selectedNode;
    }

    private void Context_OnExpanded(object sender, RfTreeNode<TTreeItemData> expandedNode)
    {
        //Currently there is nothing plan for this event. Though adding something here would be nice.
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Context != null)
        {
            Context.OnExpandedChange -= Context_OnExpanded;
            Context.OnSelectedChange -= Context_OnSelected;
        }
    }
}
