namespace RForgeDocs.Models;

public record TagInfo(string Text, string Color)
{
    public static TagInfo Razor = new TagInfo("razor", "is-danger is-outlined");
    public static TagInfo Backend = new TagInfo("backend", "is-info");
    public static TagInfo Base = new TagInfo("base class", "is-primary");
    public static TagInfo Enum = new TagInfo("enum", "is-warning");
    public static TagInfo Library = new TagInfo("library", "is-success");

    public static TagInfo Empty = new TagInfo("", "");
}
