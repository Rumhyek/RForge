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
    public RenderFragment Nodes { get; set; }

    /// <summary>
    /// Gets or sets the child content to be rendered before the nodes. Use this to render the skeleton view of this tree. To show set <see cref="ShowAsPrerender"/> to true.
    /// </summary>
    [Parameter] 
    public RenderFragment PrerenderNodes { get; set; }

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
    /// Gets or sets a value indicating whether a node can be clicked. This is seperate from being selected.
    /// </summary>
    [Parameter]
    public bool AllowClick { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the tree view is in prerender mode or not.
    /// </summary>
    [Parameter]
    public bool ShowAsPrerender { get; set; }

    private TreeViewContext<TTreeItemData> Context { get; set; }
    /// <summary>
    /// Method invoked when the component is initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        Context = new TreeViewContext<TTreeItemData>()
        {
            AllowSelection = AllowSelection,
            AllowExpand = AllowExpand,
            ShowAsPrerender = ShowAsPrerender,
            AllowNodeClick = AllowClick
        };

        Context.OnExpandedChange += Context_OnExpandedChange;
        Context.OnSelectedChange += Context_OnSelectedChange;

        base.OnInitialized();
    }

    /// <summary>
    /// Method invoked when the component has received parameters from its parent.
    /// </summary>
    protected override void OnParametersSet()
    {
        Context.AllowSelection = AllowSelection;
        Context.AllowExpand = AllowExpand;
        Context.ShowAsPrerender = ShowAsPrerender;

        base.OnParametersSet();
    }

    private Task Context_OnSelectedChange(object sender, RfTreeNode<TTreeItemData> selectedNode)
    {
        //Currently there is nothing plan for this event. Though adding something here would be nice.
        return Task.CompletedTask;
    }

    private Task Context_OnExpandedChange(object sender, RfTreeNode<TTreeItemData> expandedNode)
    {
        //Currently there is nothing plan for this event. Though adding something here would be nice.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Context != null)
        {
            Context.OnExpandedChange -= Context_OnExpandedChange;
            Context.OnSelectedChange -= Context_OnSelectedChange;
        }
    }
}
