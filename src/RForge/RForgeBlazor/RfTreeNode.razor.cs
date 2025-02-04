using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;
public partial class RfTreeNode<TTreeItemData> : ComponentBase where TTreeItemData : class
{
    [Parameter]
    public TTreeItemData NodeData { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }
    [Parameter]
    public EventCallback<bool> IsSelectedChanged { get; set; }

    [Parameter]
    public string NodeCss { get; set; }

    [Parameter]
    public bool IsExpanded { get; set; }
    [Parameter]
    public EventCallback<bool> IsExpandedChanged { get; set; }

    [Parameter]
    public bool ShowExpandIcon { get; set; }

    [Parameter]
    public string ExpandedIconCss { get; set; } = "fa-solid fa-chevron-down";
    [Parameter]
    public string CollapsedIconCss { get; set; } = "fa-solid fa-chevron-right";

    [Parameter]
    public RenderFragment<TTreeItemData> Node { get; set; }
    [Parameter]
    public RenderFragment Children { get; set; }

    [Parameter]
    public string LinkUrl { get; set; }

    [Parameter]
    public EventCallback<TTreeItemData> OnNodeClick { get; set; }

    [CascadingParameter]
    private TreeViewContext<TTreeItemData> TreeViewContext { get; set; }

    private string ChildrenId { get; set; }

    protected override void OnInitialized()
    {
        ChildrenId = Guid.NewGuid().ToString();
        base.OnInitialized();
    }

    private bool expansionChangeOcurred = false;
    private bool selectionChangeOcurred = false;
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

    protected override void OnParametersSet()
    {
        if (TreeViewContext == null)
        {
            throw new InvalidOperationException($"{nameof(RfTreeNode<TTreeItemData>)} must be a child of {nameof(RfTreeView<TTreeItemData>)}");
        }
        else
        {
            if(selectionChangeOcurred == true)
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