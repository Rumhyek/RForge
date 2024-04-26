namespace RForgeDocs.Abstractions.DataModels;

public class GridPageResults<TRowItem>
{
    public List<TRowItem> Data { get; set; } = new();
    public int TotalCount { get; set; }
}