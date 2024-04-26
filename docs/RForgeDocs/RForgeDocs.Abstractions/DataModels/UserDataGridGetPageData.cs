using RForge.Abstractions;

namespace RForgeDocs.Abstractions.DataModels;

public class UserDataGridGetPageData
{
    public int PageIndex { get; set; }
    public int? PageSize { get; set; }
    public RfSortOrder SortOrder { get; set; }
    public string SortKey { get; set; }
    public UserDataGridFilterData Filters { get; set; } = new();
}
