using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.DataGrids;
using RForgeBlazor.Models;

namespace RForgeBlazor;

public partial class RfDgHeader
{

    [CascadingParameter]
    public DataGridContext GridContext { get; set; }

    [Parameter]
    public bool AllowSorting { get; set; } = true;

    [Parameter]
    public string Css { get; set; }

    [Parameter]
    public string SortKey { get; set; }

    private RfSortOrder SortOrder { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private string HeaderCss
    {
        get
        {
            string css = "";

            if (string.IsNullOrWhiteSpace(Css) == false)
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

    private static RfSortOrder[] ToggleOrder = new RfSortOrder[]
    {
    RfSortOrder.None,
    RfSortOrder.Ascending,
    RfSortOrder.Descending
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (GridContext == null)
            throw new ArgumentNullException($"{nameof(RfDgHeader)} must be within a {nameof(GridContext)}");
        else
        {
            GridContext.OnSortChanged -= OnSortChangedCallback;
            GridContext.OnSortChanged += OnSortChangedCallback;
        }

        if (AllowSorting == true && string.IsNullOrWhiteSpace(SortKey) == true)
            throw new ArgumentNullException($"{nameof(RfDgHeader)} must asign {nameof(SortKey)} a unique value and cannot be null if {nameof(AllowSorting)} is true.");
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

        await GridContext.SortChanged(new DataGridSortBy()
        {
            SortKey = this.SortKey,
            SortOrder = ToggleOrder[index]
        });
    }

    public void Dispose()
    {
        if (GridContext != null)
        {
            GridContext.OnSortChanged -= OnSortChangedCallback;
        }
    }
}