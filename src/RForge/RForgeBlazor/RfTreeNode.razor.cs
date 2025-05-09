using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;
/// <summary>
/// Represents a tree node component.
/// </summary>
public partial class RfTreeNode : ComponentBase
{
    #region Parameters

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
    /// if null <see cref="RfTreeView.AllowSelection"/> is used. Otherwise, this value is used.
    /// </summary>
    [Parameter]
    public bool? AllowSelection { get; set; }

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
    /// if null <see cref="RfTreeView.AllowExpand"/> is used. Otherwise, this value is used.
    /// </summary>
    [Parameter]
    public bool? AllowExpand { get; set; }

    /// <summary>
    /// if null <see cref="RfTreeView.AllowClick"/> is used. Otherwise, this value is used.
    /// </summary>
    [Parameter]
    public bool? AllowClick { get; set; }

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
    /// Gets or sets the CSS class for the expanded icon.
    /// </summary>
    [Parameter]
    public string ExpandedIconCss { get; set; } = "fa-solid fa-chevron-down";
    /// <summary>
    /// Gets or sets the CSS class for the collapsed icon.
    /// </summary>
    [Parameter]
    public string IconCss { get; set; }

    /// <summary>
    /// Gets or sets the render fragment for the node.
    /// </summary>
    [Parameter]
    public RenderFragment Node { get; set; }
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
    public EventCallback<TreeViewNodeOnClickEventArgs> NodeClick { get; set; }

    /// <summary>
    /// Called when the node's <see cref="IsExpanded"/> value changes from within the component.
    /// </summary>
    [Parameter]
    public EventCallback<TreeViewNodeIsExpandEventArgs> NodeExpandChange { get; set; }
    /// <summary>
    /// Called when the node's <see cref="IsSelected"/> value changes from within the component.
    /// </summary>
    [Parameter]
    public EventCallback<TreeViewNodeIsSelectedEventArgs> NodeSelectChange { get; set; }

    #endregion

    [CascadingParameter]
    private TreeViewContext Context { get; set; }


    private bool HasChildren => Children != null;
    private string ChildrenId { get; set; }
    private bool ExpansionChangeOcurred = false;
    private bool selectionChangeOcurred = false;
    private readonly string defaultIconCss = "fa-solid fa-chevron-right";

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
            throw new InvalidOperationException($"{nameof(RfTreeNode)} must be a child of {nameof(RfTreeView)}");
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
        if (await ChangeSelection(IsSelected == false) == true)
        {
            await Context.NodeSelectionChange(this);
        }

        if (await ChangeExpansion(true) == true)
        {
            await Context.NodeExpandChange(this);
        }

        if (MyAllowClick == true)
        {
            await NodeClick.InvokeAsync(new TreeViewNodeOnClickEventArgs()
            {
                NodeReference = this
            });
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
        if (HasChildren == false)
        {
            return false;
        }

        if (MyAllowExpand == false)
        {
            return false;
        }

        if (IsExpanded == isExpanding)
        {
            return false;
        }

        IsExpanded = isExpanding;
        await IsExpandedChanged.InvokeAsync(IsExpanded); 

        StateHasChanged();

        await NodeExpandChange.InvokeAsync(new TreeViewNodeIsExpandEventArgs()
        {
            IsExpanded = IsExpanded,
            NodeReference = this
        });

        return true;
    }

    private async Task<bool> ChangeSelection(bool isSelecting)
    {
        if (MyAllowSelection == false) return false;
        if (IsSelected == isSelecting) return false;

        IsSelected = isSelecting;
        await IsSelectedChanged.InvokeAsync(IsSelected);

        StateHasChanged();

        await NodeSelectChange.InvokeAsync(new TreeViewNodeIsSelectedEventArgs()
        {
            IsSelected = IsSelected,
            NodeReference = this
        });

        return true;
    }

    private string ExpandTitleText
    {
        get
        {
            if (HasChildren == false)
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

    private bool MyAllowSelection
    {
        get
        {
            if (AllowSelection.HasValue == true)
            {
                return AllowSelection.Value;
            }

            return Context.AllowSelection;
        }
    }

    private bool MyAllowExpand
    {
        get
        {
            if(AllowExpand.HasValue == true)
            {
                return AllowExpand.Value;
            }

            return Context.AllowExpand;
        }
    }

    private bool MyAllowClick
    {
        get
        {
            if (AllowClick.HasValue == true)
            {
                return AllowClick.Value;
            }

            return Context.AllowNodeClick;
        }
    }

    private string GetIconCss()
    {
        if(HasChildren == false) 
            return IconCss;
        
        //Handle expanded
        if(IsExpanded == true)
            return ExpandedIconCss;

        //handle collapsed (reuss IconCss)
        if (string.IsNullOrWhiteSpace(IconCss) == true) 
            return defaultIconCss;

        return IconCss;
    }

    private bool HasIcon() => string.IsNullOrWhiteSpace(GetIconCss()) == false;

    private TreeNodeDisplayMode GetDisplayMode()
    {
        if(Context == null)
        {
            return TreeNodeDisplayMode.TreeViewPrerender;
        }

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