using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor;
public partial class RfTreeNode<TTreeItemData> : ComponentBase where TTreeItemData : class
{

    [Parameter]
    public TTreeItemData NodeData { get; set; }

    [Parameter]
    public bool IsActive { get; set; }

    [Parameter]
    public bool IsExpanded { get; set; }
    [Parameter]
    public EventCallback<bool> IsExpandedChanged { get; set; }

    [Parameter]
    public RenderFragment<TTreeItemData> Node { get; set; }
    [Parameter]
    public RenderFragment Children { get; set; }

    [Parameter]
    public string LinkUrl { get; set; }

    public EventCallback<TTreeItemData> OnClick { get; set; }

    private async Task OnNodeClick()
    {
        await OnClick.InvokeAsync(NodeData);
    }
}