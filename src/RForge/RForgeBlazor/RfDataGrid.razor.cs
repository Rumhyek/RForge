using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.DataGrids;
using RForgeBlazor.Models;

namespace RForgeBlazor;
public partial class RfDataGrid<TRowData>
{
    [Parameter]
    public int PreRenderColumnCount { get; set; } = 4;
    [Parameter]
    public int? PreRenderRowCount { get; set; }

    [Parameter]
    public bool AllowFilters { get; set; }
    [Parameter]
    public bool AllowSelection { get; set; }
    [Parameter]
    public int? MaxSelection { get; set; }

    [Parameter]
    public string SortKey { get; set; }
    [Parameter]
    public EventCallback<string> SortKeyChanged { get; set; }
    [Parameter]
    public RfSortOrder SortOrder { get; set; }
    [Parameter]
    public EventCallback<RfSortOrder> SortOrderChanged { get; set; }

    [Parameter]
    public IEnumerable<TRowData> Data { get; set; }
    [Parameter]
    public int TotalCount { get; set; }
    [Parameter]
    public int CurrentPageIndex { get; set; }
    [Parameter]
    public EventCallback<int> CurrentPageIndexChanged { get; set; }

    [Parameter]
    public int? PageSize { get; set; }
    [Parameter]
    public EventCallback<int?> PageSizeChanged { get; set; }

    [Parameter]
    public int MaxPagingOptions { get; set; } = 7;

    private List<TRowData> currentSelection { get; set; } = new List<TRowData>();

    [Parameter]
    public EventCallback<TRowData> OnRowSelect { get; set; }
    [Parameter]
    public EventCallback<TRowData> OnRowDeselect { get; set; }
    [Parameter]
    public EventCallback OnLoadData { get; set; }

    [Parameter]
    public RenderFragment Headers { get; set; }
    [Parameter]
    public RenderFragment Filters { get; set; }
    [Parameter]
    public RenderFragment<TRowData> Cells { get; set; }

    [Parameter]
    public string CssClass { get; set; }

    [Parameter]
    public bool Compact { get; set; } = true;

    [Parameter]
    public bool IsFullWidth { get; set; } = true;

    private DataGridContext GridContext { get; set; }

    private string ComputedTableCss
    {
        get
        {
            string css = "table";

            if (string.IsNullOrEmpty(CssClass) == false)
                css += $" {CssClass}";

            if (IsFullWidth == true)
                css += " is-fullwidth";

            if (AllowFilters == true && Filters != null)
                css += " has-filters";

            if (AllowSelection == true && Cells != null && Data != null)
                css += " is-hoverable";

            if (Compact == true)
                css += " is-striped";

            return css;
        }
    }

    private int PreRenderRowCountValue
    {
        get
        {
            if (PageSize == null && PreRenderRowCount == null) return 4;

            if (PreRenderRowCount == null)
                return PageSize.Value;

            return PreRenderRowCount.Value;
        }
    }

    public async Task ReloadData()
    {
        while (currentSelection.Count > 0)
        {
            var removed = currentSelection[currentSelection.Count - 1];
            currentSelection.RemoveAt(currentSelection.Count - 1);
            await this.OnRowDeselect.InvokeAsync(removed);
        }

        await OnLoadData.InvokeAsync();
    }

    protected override void OnInitialized()
    {
        GridContext = new DataGridContext();
        GridContext.OnSortChanged += OnSortChangedCallback;
        GridContext.OnFilterChanged += OnFilterChangedCallback;
    }

    protected override async Task OnParametersSetAsync()
    {
        await GridContext.SortChanged(new DataGridSortBy()
        {
            SortKey = this.SortKey,
            SortOrder = this.SortOrder
        });

        //TODO: if things change due to parent we will need to reload data
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender == true && Data == null)
            await ReloadData();
    }

    #region paging

    public bool ShowPaging => PageSize.HasValue;

    public int TotalPages
    {
        get
        {
            if (ShowPaging == false) return 0;

            return (int)Math.Ceiling(TotalCount / (decimal)PageSize.Value);
        }
    }

    private int[] PagingTabs
    {
        get
        {
            if (ShowPaging == false) return null;

            int totalPages = TotalPages;
            int[] pageIndexes = new int[Math.Min(totalPages, MaxPagingOptions)];

            //figure out going from start to end
            int startPageIndex = (int)Math.Max(0, Math.Ceiling((decimal)CurrentPageIndex - (pageIndexes.Length / 2)));
            int endPageIndex = Math.Min(totalPages, startPageIndex + pageIndexes.Length);

            //correct for at the end to push the pager back
            startPageIndex = Math.Max(0, startPageIndex - (MaxPagingOptions - (endPageIndex - startPageIndex)));

            for (int p = 0; p < pageIndexes.Length; p++)
            {
                pageIndexes[p] = startPageIndex + p;
            }

            return pageIndexes;
        }
    }

    public async Task OnFirstPageClick()
    {
        await OnPageIndexClick(0);
    }

    public async Task OnPreviousPageClick()
    {
        await OnPageIndexClick(Math.Max(0, CurrentPageIndex - 1));
    }

    public async Task OnNextPageClick()
    {
        await OnPageIndexClick(Math.Min(TotalPages - 1, CurrentPageIndex + 1));
    }
    public async Task OnLastPageClick()
    {
        await OnPageIndexClick(TotalPages - 1);
    }

    public async Task OnPageIndexClick(int pageIndex)
    {
        if (pageIndex == CurrentPageIndex) return;

        await CurrentPageIndexChanged.InvokeAsync(pageIndex);

        await ReloadData();
    }

    #endregion

    #region Selection

    private async Task OnRowClick(TRowData row)
    {
        if (AllowSelection == false || MaxSelection <= 0) return;

        if (currentSelection.Contains(row) == true)
        {
            currentSelection.Remove(row);
            await OnRowDeselect.InvokeAsync(row);
        }
        else
        {
            if (MaxSelection != null && currentSelection.Count + 1 > MaxSelection)
            {
                var deselectFirst = currentSelection.First();
                currentSelection.RemoveAt(0);
                await OnRowDeselect.InvokeAsync(deselectFirst);
            }

            currentSelection.Add(row);
            await OnRowSelect.InvokeAsync(row);
        }
    }

    #endregion

    private async Task OnFilterChangedCallback(object caller, DataGridFilterBy filter)
    {
        if (ShowPaging == true && CurrentPageIndex != 0)
            await CurrentPageIndexChanged.InvokeAsync(0);

        await ReloadData();
    }

    private async Task OnSortChangedCallback(object caller, DataGridSortBy sortBy)
    {
        bool hasChanged = false;
        if (sortBy.SortKey != this.SortKey)
        {
            await this.SortKeyChanged.InvokeAsync(sortBy.SortKey);
            hasChanged = true;
        }

        if (sortBy.SortOrder != this.SortOrder)
        {
            await this.SortOrderChanged.InvokeAsync(sortBy.SortOrder);
            hasChanged = true;
        }

        if (hasChanged == true)
        {

            if (ShowPaging == true && CurrentPageIndex != 0)
                await CurrentPageIndexChanged.InvokeAsync(0);

            await ReloadData();
        }
    }

    public void Dispose()
    {
        if (GridContext != null)
        {
            GridContext.OnSortChanged -= OnSortChangedCallback;
            GridContext.OnFilterChanged -= OnFilterChangedCallback;
        }
    }

}