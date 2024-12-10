
/// <summary>
/// A base example using simple straight forward logic
/// </summary>
public static class BasicForeach
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
}
