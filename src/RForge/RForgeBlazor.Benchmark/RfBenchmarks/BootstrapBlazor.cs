
/// <summary>
/// Based off of <see href="https://github.dev/vikramlearning/blazorbootstrap"/>
/// </summary>
public static class BootstrapBlazor
{
    public static string ClassWhen(params (string cssClass, bool when)[] cssClassList)
    {
        var list = new HashSet<string>();

        if (cssClassList is not null && cssClassList.Any())
            foreach (var (cssClass, when) in cssClassList)
            {
                if (!string.IsNullOrWhiteSpace(cssClass) && when)
                    list.Add(cssClass);
            }

        if (list.Any())
            return string.Join(" ", list);
        else
            return string.Empty;
    }
}