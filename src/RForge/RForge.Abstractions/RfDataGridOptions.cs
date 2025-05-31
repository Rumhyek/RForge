using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForge.Abstractions;

public class RfDataGridOptions
{
    /// <summary>
    /// How many columns to render while showing the skeleton mode. Deafult = 4
    /// </summary>
    public int PreRenderColumnCount { get; set; } = 4;

    /// <summary>
    /// How many rows to render while showing the skeleton mode.  Default = PageSize or 4 if both are null.
    /// </summary>
    public int? PreRenderRowCount { get; set; }

    /// <summary>
    /// If true shows the filter row.
    /// </summary>
    public bool AllowFilters { get; set; } = true;

    /// <summary>
    /// If true adds support for <see cref="OnRowSelect"/> and <see cref="OnRowDeselect"/>
    /// </summary>
    public bool AllowSelection { get; set; }

    /// <summary>
    /// If set the tells the Data Grid what the maximum selected rows.
    /// </summary>
    public int? MaxSelection { get; set; }

    /// <summary>
    /// The max paging options to show in the bottom left. By default = 0
    /// </summary>
    public int MaxPagingOptions { get; set; } = 0;

    /// <summary>
    /// Sets the click delay for double click. This is used to determine if a click is a double click or not. Default = 250ms.
    /// </summary>
    public int DoubleClickDelay { get; set; } = 250;

    /// <summary>
    /// If true the selection will be kept when the data is reloaded. Default = false.
    /// </summary>
    public bool KeepSelection { get; set; } = false;
}

public class RfDataGridStyles
{
    /// <summary>
    /// Should the table have stripes or not. Default = true.
    /// </summary>
    public bool IsStriped { get; set; } = true;

    /// <summary>
    /// Should the table take up 100% of the space or not. Default = true.
    /// </summary>
    public bool IsFullWidth { get; set; } = true;
    
    /// <summary>
    /// The css class to add to the empty content message.
    /// </summary>
    public string EmptyContentCss { get; set; } = "has-text-centered has-text-grey has-text-weight-bold";

    /// <summary>
    /// A Css class to add to the base table.
    /// </summary>
    public string CssClass { get; set; }

    /// <summary>
    /// A Css class to add to the pager wrapping div.
    /// </summary>
    public string PagerCssClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class applied to the current page in a pagination control. Default is "is-primary".
    /// </summary>
    public string CurrentPagingCssClass { get; set; } = "is-primary";
}

public class RfDataGridModel<TRowData>
{

    /// <summary>
    /// The current sort direction
    /// </summary>
    public RfSortOrder SortOrder { get; set; }

    /// <summary>
    /// The data rows to display within the data grid. If null the skeleton of the data grid will be shown. 
    /// </summary>
    public IEnumerable<TRowData> Data { get; set; }

    /// <summary>
    /// The max number or rows for any given page.
    /// </summary>
    public int? PageSize { get; set; }
}