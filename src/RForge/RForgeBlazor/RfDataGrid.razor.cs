using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.DataGrids;
using RForgeBlazor.Models;

namespace RForgeBlazor;
public partial class RfDataGrid<TRowData>
{
    #region Parameters
    /// <summary>
    /// How many columns to render while showing the skeleton mode. Deafult = 4
    /// </summary>
    [Parameter]
    public int PreRenderColumnCount { get; set; } = 4;

    /// <summary>
    /// How many rows to render while showing the skeleton mode.  Default = PageSize or 4 if both are null.
    /// </summary>
    [Parameter]
    public int? PreRenderRowCount { get; set; }


    /// <summary>
    /// If true shows the filter row.
    /// </summary>
    [Parameter]
    public bool AllowFilters { get; set; } = true;

    /// <summary>
    /// If true adds support for <see cref="OnRowSelect"/> and <see cref="OnRowDeselect"/>
    /// </summary>
    [Parameter]
    public bool AllowSelection { get; set; }

    /// <summary>
    /// If set the tells the Data Grid what the maximum selected rows.
    /// </summary>
    [Parameter]
    public int? MaxSelection { get; set; }

    /// <summary>
    /// The current sort key the grid is filtering with.
    /// </summary>
    [Parameter]
    public string SortKey { get; set; }
    [Parameter]
    public EventCallback<string> SortKeyChanged { get; set; }

    /// <summary>
    /// The current sort direction
    /// </summary>
    [Parameter]
    public RfSortOrder SortOrder { get; set; }
    [Parameter]
    public EventCallback<RfSortOrder> SortOrderChanged { get; set; }

    /// <summary>
    /// The data rows to display within the data grid. If null the skeleton of the data grid will be shown. 
    /// </summary>
    [Parameter]
    public IEnumerable<TRowData> Data { get; set; }

    /// <summary>
    /// The total row count. All rows not just the ones displayed for the current page.
    /// </summary>
    [Parameter]
    public int TotalCount { get; set; }

    /// <summary>
    /// The current page index. 0 based.
    /// </summary>
    [Parameter]
    public int CurrentPageIndex { get; set; }
    [Parameter]
    public EventCallback<int> CurrentPageIndexChanged { get; set; }

    /// <summary>
    /// The max number or rows for any given page.
    /// </summary>
    [Parameter]
    public int? PageSize { get; set; }
    [Parameter]
    public EventCallback<int?> PageSizeChanged { get; set; }

    /// <summary>
    /// The max paging options to show in the bottom left. By default = 7
    /// </summary>
    [Parameter]
    public int MaxPagingOptions { get; set; } = 7;


    /// <summary>
    /// Fires when a row is selected return the value of the row.
    /// </summary>
    [Parameter]
    public EventCallback<TRowData> OnRowSelect { get; set; }

    /// <summary>
    /// Fires when a row is deselected returning the value of the row.
    /// </summary>
    [Parameter]
    public EventCallback<TRowData> OnRowDeselect { get; set; }

    /// <summary>
    /// Called when the data grid believes new data should be shown.
    /// </summary>
    [Parameter]
    public EventCallback OnLoadData { get; set; }

    /// <summary>
    /// The template section to add headers. Use <see cref="RfDgHeader"/>
    /// </summary>
    [Parameter]
    public RenderFragment Headers { get; set; }

    /// <summary>
    /// The template section to add filters. Use <see cref="RfDgFilterInput{TType}"/> or use <see cref="RfDgFilterNone"/> for blank spaces.
    /// </summary>
    [Parameter]
    public RenderFragment Filters { get; set; }

    /// <summary>
    /// The template section to create the table cells. Use <see cref="RfDgCell{TRowData}"/>
    /// </summary>
    [Parameter]
    public RenderFragment<TRowData> Cells { get; set; }

    /// <summary>
    /// A Css class to add to the base table.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// A Css class to add to the pager wrapping div.
    /// </summary>
    [Parameter]
    public string PagerCssClass { get; set; }

    /// <summary>
    /// Should the table have stripes or not. Default = true.
    /// </summary>
    [Parameter]
    public bool IsStriped { get; set; } = true;

    /// <summary>
    /// Should the table take up 100% of the space or not. Default = true.
    /// </summary>
    [Parameter]
    public bool IsFullWidth { get; set; } = true;
    #endregion

    private List<TRowData> currentSelection { get; set; } = new List<TRowData>();

    private DataGridContext GridContext { get; set; }

    private bool onParameterSetUpdateSortOrder { get; set; }
    private bool onParameterSetReloadData { get; set; }

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

            if (IsStriped == true)
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
        GridContext = new DataGridContext()
        {
            InitialSortOrder = SortOrder,
            InitialSortKey = SortKey
        };
        GridContext.OnSortChanged += OnSortChangedCallback;
        GridContext.OnFilterChanged += OnFilterChangedCallback;
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        onParameterSetReloadData = false;
        onParameterSetUpdateSortOrder = false;

        foreach (var parameter in parameters)
        {
            switch (parameter.Name)
            {
                case nameof(PreRenderColumnCount):
                    PreRenderColumnCount = (int)parameter.Value;
                    break;
                case nameof(PreRenderRowCount):
                    PreRenderRowCount = (int?)parameter.Value;
                    break;
                case nameof(AllowFilters):
                    AllowFilters = (bool)parameter.Value;
                    break;
                case nameof(AllowSelection):
                    AllowSelection = (bool)parameter.Value;
                    break;
                case nameof(MaxSelection):
                    MaxSelection = (int?)parameter.Value;
                    break;
                case nameof(SortKey):
                    onParameterSetUpdateSortOrder |= SortKey != (string)parameter.Value;
                    SortKey = (string)parameter.Value;
                    break;
                case nameof(SortKeyChanged):
                    SortKeyChanged = (EventCallback<string>)parameter.Value;
                    break;
                case nameof(SortOrderChanged):
                    SortOrderChanged = (EventCallback<RfSortOrder>)parameter.Value;
                    break;
                case nameof(SortOrder):
                    onParameterSetUpdateSortOrder |= SortOrder != (RfSortOrder)parameter.Value;
                    SortOrder = (RfSortOrder)parameter.Value;
                    break;
                case nameof(Data):
                    Data = (IEnumerable<TRowData>)parameter.Value;
                    break;
                case nameof(TotalCount):
                    TotalCount = (int)parameter.Value;
                    break;
                case nameof(CurrentPageIndex):
                    onParameterSetReloadData |= CurrentPageIndex != (int?)parameter.Value;
                    CurrentPageIndex = (int)parameter.Value;
                    break;
                case nameof(CurrentPageIndexChanged):
                    CurrentPageIndexChanged = (EventCallback<int>)parameter.Value;
                    break;
                case nameof(PageSize):
                    onParameterSetReloadData |= PageSize != (int?)parameter.Value;
                    PageSize = (int?)parameter.Value;
                    break;
                case nameof(PageSizeChanged):
                    PageSizeChanged = (EventCallback<int?>)parameter.Value;
                    break;
                case nameof(MaxPagingOptions):
                    MaxPagingOptions = (int)parameter.Value;
                    break;
                case nameof(OnRowSelect):
                    OnRowSelect = (EventCallback<TRowData>)parameter.Value;
                    break;
                case nameof(OnRowDeselect):
                    OnRowDeselect = (EventCallback<TRowData>)parameter.Value;
                    break;
                case nameof(OnLoadData):
                    OnLoadData = (EventCallback)parameter.Value;
                    break;
                case nameof(Headers):
                    Headers = (RenderFragment)parameter.Value;
                    break;
                case nameof(Filters):
                    Filters = (RenderFragment)parameter.Value;
                    break;
                case nameof(Cells):
                    Cells = (RenderFragment<TRowData>)parameter.Value;
                    break;
                case nameof(CssClass):
                    CssClass = (string)parameter.Value;
                    break;
                case nameof(PagerCssClass):
                    PagerCssClass = (string)parameter.Value;
                    break;
                case nameof(IsStriped):
                    IsStriped = (bool)parameter.Value;
                    break;
                case nameof(IsFullWidth):
                    IsFullWidth = (bool)parameter.Value;
                    break;
            }
        }

        //If the sort changed we need to reload the data.
        onParameterSetReloadData |= onParameterSetUpdateSortOrder;

        //don't reload on the initialize
        //onParameterSetUpdateSortOrder &= GridContext != null;
        onParameterSetReloadData &= GridContext != null;

        await base.SetParametersAsync(ParameterView.Empty);
    }


    protected override async Task OnParametersSetAsync()
    {
        if(onParameterSetUpdateSortOrder == true)
        {
            //The parent component updated the parameter. Notify the children
            await GridContext.SortChanged(new DataGridSortBy()
            {
                SortKey = SortKey,
                SortOrder = SortOrder
            });
        }

        if(onParameterSetReloadData == true)
            await OnLoadData.InvokeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender == true && Data == null)
            await ReloadData();
    }

    #region paging

    public bool ShowPaging => PageSize.HasValue && PageSize.Value > 0;

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

        CurrentPageIndex = pageIndex;
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
            SortKey = sortBy.SortKey;
            await this.SortKeyChanged.InvokeAsync(sortBy.SortKey);
            hasChanged = true;
        }

        if (sortBy.SortOrder != this.SortOrder)
        {
            SortOrder = sortBy.SortOrder;
            await this.SortOrderChanged.InvokeAsync(sortBy.SortOrder);
            hasChanged = true;
        }

        if (hasChanged == true)
        {

            if (ShowPaging == true && CurrentPageIndex != 0)
            {
                CurrentPageIndex = 0;
                await CurrentPageIndexChanged.InvokeAsync(0);
            }

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