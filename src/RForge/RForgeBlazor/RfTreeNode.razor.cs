using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;
/// <summary>
/// Represents a tree node component.
/// </summary>
/// <typeparam name="TTreeItemData">The type of the tree item data.</typeparam>
public partial class RfTreeNode<TTreeItemData> : ComponentBase where TTreeItemData : class
{
    #region Parameters
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
    /// Gets or sets a value indicating whether the node is loading.
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }
    /// <summary>
    /// Gets or sets the number of loading nodes to display while loading.
    /// </summary>
    [Parameter]
    public int LoadingNodeCount { get; set; } = 3;

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

    /// <summary>
    /// Called when the node's <see cref="IsExpanded"/> value changes from within the component.
    /// </summary>
    [Parameter]
    public EventCallback<TreeViewNodeIsExpandEventArgs<TTreeItemData>> NodeExpandChange { get; set; }
    /// <summary>
    /// Called when the node's <see cref="IsSelected"/> value changes from within the component.
    /// </summary>
    [Parameter]
    public EventCallback<TreeViewNodeIsSelectedEventArgs<TTreeItemData>> NodeSelectChange { get; set; }

    #endregion

    [CascadingParameter]
    private TreeViewContext<TTreeItemData> Context { get; set; }

    private string ChildrenId { get; set; }
    private bool ExpansionChangeOcurred = false;
    private bool selectionChangeOcurred = false;

    /// <summary>
    /// Method invoked when the component is initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        ChildrenId = Guid.NewGuid().ToString();
        base.OnInitialized();
    }

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
            ExpansionChangeOcurred = isExpanded != IsExpanded;
        }

        await base.SetParametersAsync(parameters);
    }

    /// <summary>
    /// Method invoked when the component's parameters are set.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (Context == null)
        {
            throw new InvalidOperationException($"{nameof(RfTreeNode<TTreeItemData>)} must be a child of {nameof(RfTreeView<TTreeItemData>)}");
        }
        else
        {
            if (selectionChangeOcurred == true)
            {
                await Context.NodeSelectionChange(this);
            }

            if (ExpansionChangeOcurred == true)
            {
                await Context.NodeExpandChange(this);
            }
        }

        await base.OnParametersSetAsync();
    }

    private async Task OnNodeClickCallback()
    {
        if (await ChangeSelection(true) == true)
        {
            await Context.NodeSelectionChange(this);
        }

        if (await ChangeExpansion(true) == true)
        {
            await Context.NodeExpandChange(this);
        }

        if (Context.AllowSelection == true)
        {
            await OnNodeClick.InvokeAsync(NodeData);
        }
    }

    private async Task OnToggleExpandClick()
    {
        if (await ChangeExpansion(IsExpanded == false) == true)
        {
            await Context.NodeExpandChange(this);
        }
    }

    private async Task<bool> ChangeExpansion(bool isExpanding)
    {
        if (ShowExpandIcon == false)
        {
            return false;
        }

        if (Context.AllowExpand == false)
        {
            return false;
        }

        if (IsExpanded == isExpanding)
        {
            return false;
        }

        IsExpanded = isExpanding;

        StateHasChanged();

        await NodeExpandChange.InvokeAsync(new TreeViewNodeIsExpandEventArgs<TTreeItemData>()
        {
            IsExpanded = IsExpanded,
            NodeData = NodeData,
            NodeReference = this
        });

        return true;
    }

    private async Task<bool> ChangeSelection(bool isSelecting)
    {
        if (Context.AllowSelection == false) return false;
        if (IsSelected == isSelecting) return false;

        IsSelected = isSelecting;

        StateHasChanged();

        await NodeSelectChange.InvokeAsync(new TreeViewNodeIsSelectedEventArgs<TTreeItemData>()
        {
            IsSelected = IsSelected,
            NodeData = NodeData,
            NodeReference = this
        });

        return true;
    }

    internal async Task Deselect()
    {
        if(await ChangeSelection(false) == true)
            await Context.NodeSelectionChange(this);
    }

    internal async Task Collapse()
    {
        if (await ChangeExpansion(false) == true)
            await Context.NodeExpandChange(this);
    }

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

    private TreeNodeDisplayMode GetDisplayMode()
    {
        if (Context.ShowAsPrerender == true)
        {
            return TreeNodeDisplayMode.TreeViewPrerender;
        }

        if (IsLoading == true)
        {
            return TreeNodeDisplayMode.Loading;
        }

        return TreeNodeDisplayMode.Display;
    }

    private enum TreeNodeDisplayMode
    {
        TreeViewPrerender,
        Loading,
        Display
    }

}