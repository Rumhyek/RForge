
/// <summary>
/// A base example using simple straight forward logic
/// </summary>
public static class RfBasicForeach
{
    public static string ClassWhen(params (string className, bool show)[] cssClassList)
    {
        string classes = "";
        bool isFirst = true;
        foreach (var css in cssClassList)
        {
            if (css.show == false || string.IsNullOrWhiteSpace(css.className) == true)
                continue;

            if (isFirst == false)
                classes += " ";

            classes += css.className;
            isFirst = false;
        }

        return classes;
    }
    public static string Class(params string[] classList)
    {
        if(classList == null || classList.Length == 0)
            return string.Empty;

        return string.Join(' ', new HashSet<string>(classList));
    }

    public static string StyleWhen(params (string styleName, string value, bool show)[] styles)
    {
        string output = "";
        
        foreach (var style in styles)
        {
            if (style.show == false
                || string.IsNullOrWhiteSpace(style.styleName) == true
                || string.IsNullOrWhiteSpace(style.value) == true) 
                continue;

            output += $"{style.styleName}:{style.value};";
        }

        return output;
    }
}
