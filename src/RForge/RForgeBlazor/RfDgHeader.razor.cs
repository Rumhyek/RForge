using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.DataGrids;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// Wraps a th providing support for sorting columns.
/// </summary>
/// <example>
/// <code>
/// &lt;RfDgHeader SortKey="Name" CssClass="custom-header" AllowSorting="true"&gt;
///     &lt;span&gt;Name&lt;/span&gt;
/// &lt;/RfDgHeader&gt;
/// </code>
/// </example>
public partial class RfDgHeader
{
    #region Parameters
    /// <summary>
    /// The needed context to communicate between the data grid and the header for sorting.
    /// </summary>
    [CascadingParameter]
    public DataGridContext GridContext { get; set; }

    /// <summary>
    /// Allows this header to be clickable and send out <see cref="DataGridContext.OnSortChanged"/>.
    /// </summary>
    [Parameter]
    public bool AllowSorting { get; set; } = true;

    /// <summary>
    /// Any custom class to add to the header's &lt;/th&gt;
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// The sort key that reperesents this header.
    /// </summary>
    [Parameter]
    public string SortKey { get; set; }
    /// <summary>
    /// The content to be rendered inside the header.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }
    #endregion

    /// <summary>
    /// The current sort order of the header.
    /// </summary>
    private RfSortOrder SortOrder { get; set; }

    /// <summary>
    /// The possible sort orders to toggle through.
    /// </summary>
    private static RfSortOrder[] ToggleOrder =
    [
        RfSortOrder.None,
        RfSortOrder.Ascending,
        RfSortOrder.Descending
    ];

    /// <summary>
    /// Initializes the component.
    /// </summary>
    protected override void OnInitialized()
    {
        if (SortKey == GridContext.InitialSortKey)
            SortOrder = GridContext.InitialSortOrder;

        base.OnInitialized();
    }

    /// <summary>
    /// Sets the parameters for the component. Requires <see cref="GridContext"/> to be set as well as <see cref="SortKey"/> if <see cref="AllowSorting"/> is true.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (AllowSorting == true && string.IsNullOrWhiteSpace(SortKey) == true)
            throw new ArgumentNullException($"{nameof(RfDgHeader)} must asign {nameof(SortKey)} a unique value and cannot be null if {nameof(AllowSorting)} is true.");

        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgHeader)} must be within a {nameof(GridContext)}");
        else
        {
            GridContext.OnSortChanged -= OnSortChangedCallback;
            GridContext.OnSortChanged += OnSortChangedCallback;
        }
    }

    /// <summary>
    /// Callback for when the sort order changes. 
    /// </summary>
    /// <param name="caller">The caller object.</param>
    /// <param name="sortBy">The sort details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task OnSortChangedCallback(object caller, DataGridSortBy sortBy)
    {
        if (this.SortKey != sortBy.SortKey)
        {
            if (this.SortOrder != RfSortOrder.None)
            {
                this.SortOrder = RfSortOrder.None;
            }
            return Task.CompletedTask;
        }
        if (this.SortOrder == sortBy.SortOrder) return Task.CompletedTask;


        this.SortOrder = sortBy.SortOrder;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the click event on the column header.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task OnColumnClick()
    {
        if (AllowSorting == false) return;

        int index = Array.IndexOf(ToggleOrder, SortOrder);
        index = (index + 1) % ToggleOrder.Length;

        SortOrder = ToggleOrder[index];
        await GridContext.SortChanged(new DataGridSortBy()
        {
            SortKey = this.SortKey,
            SortOrder = SortOrder
        });
    }

    /// <summary>
    /// Disposes the component and detaches event handlers.
    /// </summary>
    public void Dispose()
    {
        if (GridContext != null)
        {
            GridContext.OnSortChanged -= OnSortChangedCallback;
        }
    }

    #region Computeds

    /// <summary>
    /// Gets the CSS classes for the header.
    /// </summary>
    private string HeaderCss
    {
        get
        {
            string css = "";

            if (string.IsNullOrWhiteSpace(CssClass) == false)
                css += $"{css} ";

            if (AllowSorting == true)
                css += "sortable ";

            switch (this.SortOrder)
            {
                case RfSortOrder.Ascending:
                    css += "sort-asc ";
                    break;
                case RfSortOrder.Descending:
                    css += "sort-desc ";
                    break;
            }

            return css;
        }
    }
    #endregion

}