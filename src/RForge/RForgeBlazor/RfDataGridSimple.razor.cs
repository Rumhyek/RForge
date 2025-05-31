using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.DataGrids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor;

public partial class RfDataGridSimple<TRowData>
{
    #region Parameters

    [Parameter]
    public RfDataGridOptions Config { get; set; } = new RfDataGridOptions();

    [Parameter] 
    public RfDataGridStyles Style { get; set; } = new RfDataGridStyles();

    /// <summary>
    /// The current sort key the grid is filtering with.
    /// </summary>
    [Parameter]
    public string SortKey { get; set; }
    /// <summary>
    /// Fires when the sort key changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> SortKeyChanged { get; set; }

    /// <summary>
    /// The current sort direction
    /// </summary>
    [Parameter]
    public RfSortOrder SortOrder { get; set; }
    /// <summary>
    /// Fires when the sort order changes.
    /// </summary>
    [Parameter]
    public EventCallback<RfSortOrder> SortOrderChanged { get; set; }

    /// <summary>
    /// The data rows to display within the data grid. If null the skeleton of the data grid will be shown. 
    /// </summary>
    [Parameter]
    public IEnumerable<TRowData> Data { get; set; }

    /// <summary>
    /// The current page index. 0 based.
    /// </summary>
    [Parameter]
    public int CurrentPageIndex { get; set; }
    /// <summary>
    /// Fires when the current page index changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> CurrentPageIndexChanged { get; set; }

    /// <summary>
    /// The max number or rows for any given page.
    /// </summary>
    [Parameter]
    public int? PageSize { get; set; }
    /// <summary>
    /// Fires when the page size changes.
    /// </summary>
    [Parameter]
    public EventCallback<int?> PageSizeChanged { get; set; }

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
    /// Fires when a row is clicked returning the value of the row. Is called after <see cref="OnRowSelect"/> and <see cref="OnRowDeselect"/> events.
    /// </summary>
    [Parameter]
    public EventCallback<TRowData> OnRowClick { get; set; }

    /// <summary>
    /// Fires when a row is double clicked returning the value of the row. Is called after <see cref="OnRowSelect"/> and <see cref="OnRowDeselect"/> events.
    /// </summary>
    [Parameter]
    public EventCallback<TRowData> OnRowDoubleClick { get; set; }

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
    /// The template section to create the table cells. Uses <typeparamref name="TRowData"/>
    /// </summary>
    [Parameter]
    public RenderFragment<TRowData> Cells { get; set; }

    /// <summary>
    /// The template section to add a custom message when <see cref="Data"/> is empty but not null.
    /// </summary>
    [Parameter]
    public RenderFragment EmptyContent { get; set; }

    /// <summary>
    /// The current selection of rows.
    /// </summary>
    [Parameter]
    public List<TRowData> CurrentSelection { get; set; } = new List<TRowData>();

    /// <summary>
    /// Fires when the current selection changes.
    /// </summary>
    [Parameter]
    public EventCallback<List<TRowData>> CurrentSelectionChanged { get; set; }

    /// <summary>  
    /// Used to compare two rows to determine if they are the same. If not set, it will use <see cref="object.Equals(object?)"/> to compare the two rows.  
    /// </summary>  
    [Parameter]
    public Func<TRowData, TRowData, bool> RowComparer { get; set; }

    /// <summary>
    /// The total row count. All rows not just the ones displayed for the current page.
    /// </summary>
    [Parameter]
    public int TotalCount { get; set; }

    #endregion

}
