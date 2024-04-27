using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.DataGrids;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// Wraps a th providing support for sorting columns.
/// </summary>
public partial class RfDgHeader
{
    #region Parameters
    [CascadingParameter]
    /// <summary>
    /// The needed context to communicate between the data grid and the header for sorting.
    /// </summary>
    public DataGridContext GridContext { get; set; }

    [Parameter]
    /// <summary>
    /// Allows this header to be clickable and send out <see cref="DataGridContext.OnSortChanged"/>.
    /// </summary>
    public bool AllowSorting { get; set; } = true;

    [Parameter]
    /// <summary>
    /// Any custom class to add to the header's <th>
    /// </summary>
    public string CssClass { get; set; }

    [Parameter]
    /// <summary>
    /// The sort key that reperesents this header.
    /// </summary>
    public string SortKey { get; set; }
    [Parameter]
    public RenderFragment ChildContent { get; set; }
    #endregion

    private RfSortOrder SortOrder { get; set; }


    private static RfSortOrder[] ToggleOrder =
    [
        RfSortOrder.None,
        RfSortOrder.Ascending,
        RfSortOrder.Descending
    ];

    protected override void OnInitialized()
    {
        if (SortKey == GridContext.InitialSortKey)
            SortOrder = GridContext.InitialSortOrder;

        base.OnInitialized();
    }

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

    public void Dispose()
    {
        if (GridContext != null)
        {
            GridContext.OnSortChanged -= OnSortChangedCallback;
        }
    }

    #region Computeds

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