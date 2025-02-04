using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;
/// <summary>
/// Represents a tree node component.
/// </summary>
/// <typeparam name="TTreeItemData">The type of the tree item data.</typeparam>
public partial class RfTreeNode<TTreeItemData> : ComponentBase where TTreeItemData : class
{
    /// <summary>
    /// Gets or sets the node data.
    /// </summary>
    [Parameter]
    public TTreeItemData NodeData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is selected.
    /// </summary>
    [Parameter]
    public bool IsSelected { get; set; }
    /// <summary>
    /// Gets or sets a callback that is invoked when the selection state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsSelectedChanged { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for the node.
    /// </summary>
    [Parameter]
    public string NodeCss { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is expanded.
    /// </summary>
    [Parameter]
    public bool IsExpanded { get; set; }
    /// <summary>
    /// Gets or sets a callback that is invoked when the expanded state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsExpandedChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the expand icon.
    /// </summary>
    [Parameter]
    public bool ShowExpandIcon { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for the expanded icon.
    /// </summary>
    [Parameter]
    public string ExpandedIconCss { get; set; } = "fa-solid fa-chevron-down";
    /// <summary>
    /// Gets or sets the CSS class for the collapsed icon.
    /// </summary>
    [Parameter]
    public string CollapsedIconCss { get; set; } = "fa-solid fa-chevron-right";

    /// <summary>
    /// Gets or sets the render fragment for the node.
    /// </summary>
    [Parameter]
    public RenderFragment<TTreeItemData> Node { get; set; }
    /// <summary>
    /// Gets or sets the render fragment for the children.
    /// </summary>
    [Parameter]
    public RenderFragment Children { get; set; }

    /// <summary>
    /// Gets or sets the URL for the link.
    /// </summary>
    [Parameter]
    public string LinkUrl { get; set; }

    /// <summary>
    /// Gets or sets the callback for the node click event.
    /// </summary>
    [Parameter]
    public EventCallback<TTreeItemData> OnNodeClick { get; set; }

    [CascadingParameter]
    private TreeViewContext<TTreeItemData> TreeViewContext { get; set; }

    private string ChildrenId { get; set; }

    /// <summary>
    /// Method invoked when the component is initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        ChildrenId = Guid.NewGuid().ToString();
        base.OnInitialized();
    }

    private bool expansionChangeOcurred = false;
    private bool selectionChangeOcurred = false;
    /// <summary>
    /// Sets parameters supplied by the caller.
    /// </summary>
    /// <param name="parameters">The parameters to set.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue(nameof(IsSelected), out bool isSelected))
        {
            selectionChangeOcurred = isSelected != IsSelected;
        }
        if (parameters.TryGetValue(nameof(IsExpanded), out bool isExpanded))
        {
            expansionChangeOcurred = isExpanded != IsExpanded;
        }

        await base.SetParametersAsync(parameters);
    }

    /// <summary>
    /// Method invoked when the component's parameters are set.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (TreeViewContext == null)
        {
            throw new InvalidOperationException($"{nameof(RfTreeNode<TTreeItemData>)} must be a child of {nameof(RfTreeView<TTreeItemData>)}");
        }
        else
        {
            if (selectionChangeOcurred == true)
                TreeViewContext.NodeSelectionChange(this);

            if (expansionChangeOcurred == true)
                TreeViewContext.NodeExpandChange(this);
        }

        base.OnParametersSet();
    }

    private async Task OnNodeClickCallback()
    {
        if (TreeViewContext.AllowSelection == true && IsSelected == false)
        {
            IsSelected = true;
            TreeViewContext.NodeSelectionChange(this);
        }

        if (TreeViewContext.AllowExpand == true && IsExpanded == false)
        {
            IsExpanded = true;
            TreeViewContext.NodeExpandChange(this);
        }

        if (TreeViewContext.AllowSelection == true)
        {
            await OnNodeClick.InvokeAsync(NodeData);
        }
    }

    private void ToggleExpand()
    {
        if (ShowExpandIcon == false)
        {
            return;
        }

        if (TreeViewContext.AllowExpand == false)
        {
            return;
        }

        IsExpanded = IsExpanded == false;
    }

    internal void Deselect() => IsSelected = false;

    internal void Collapse() => IsExpanded = false;

    private string ExpandTitleText
    {
        get
        {
            if (ShowExpandIcon == false)
            {
                return null;
            }

            if (IsExpanded == true)
            {
                return "Click to collapse";
            }

            return "Click to expand";
        }
    }

}