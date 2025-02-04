using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;

public partial class RfTreeView<TTreeItemData> : ComponentBase, IDisposable where TTreeItemData : class
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string CssClass { get; set; }

    [Parameter]
    public bool AllowSelection { get; set; } = true;

    [Parameter]
    public bool AllowExpand { get; set; } = true;

    [Parameter]
    public bool AllowMultiExpand { get; set; } = true;

    private TreeViewContext<TTreeItemData> Context { get; set; }
    private RfTreeNode<TTreeItemData> SelectedNode { get; set; }
    private RfTreeNode<TTreeItemData> ExpandedNode { get; set; }


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

    public void Dispose()
    {
        if (Context != null)
        {
            Context.OnExpandedChange -= Context_OnExpanded;
            Context.OnSelectedChange -= Context_OnSelected;
        }
    }
}