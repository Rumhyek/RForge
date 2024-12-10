using System.Runtime.InteropServices;

namespace RForgeBlazor;
/// <summary>
/// A helper class for Blazor related support.
/// </summary>
public static class Rf
{
    /// <summary>
    /// Concatenates classes together based on when show is true. 
    /// </summary>
    /// <param name="classes"></param>
    /// <returns>A single string of classes to show</returns>
    public static string ClassWhen(params (string cssClass, bool show)[] classes)
    {
        if (classes == null || classes.Length == 0)
            return string.Empty;
        
        int length = classes
            .Where(c => c.show == true && string.IsNullOrWhiteSpace(c.cssClass) == false)
            .Select(s => s.cssClass.Length)
            .Sum() + classes.Length - 1;

        if (length == 0) return String.Empty;

        // Using Span<T> for stack allocation and avoiding heap allocations
        Span<char> buffer = stackalloc char[length];

        int offset = 0;
        bool first = true;

        foreach ((string cssClass, bool show) in classes)
        {
            if (show == false || string.IsNullOrWhiteSpace(cssClass) == true)
                continue;
            
            if (first == false)
            {
                // Add space between classes only after the first class (optimized for single class)
                buffer[offset++] = ' ';
            }

            // Copy class name to buffer
            cssClass.AsSpan().CopyTo(buffer.Slice(offset));
            offset += cssClass.Length;
            first = false;
        }

        if (offset > 0)
            return new string(buffer.Slice(0, offset));

        return string.Empty;
    }

    /// <summary>
    /// Concatenates styles together based on when show is true. Only returns styles that are not null or whitespace in either style name or value. 
    /// </summary>
    /// <example>
    /// 
    /// string styleValue = Rf.StyleWhen(("color", "red", true));
    /// // styleValue = "color: red;"
    /// 
    /// styleValue = Rf.StyleWhen(("color", "red", true), ("background-color", "#001100", true));
    /// //styleValue = "color: red;background-color:#001100;"
    /// 
    /// </example>
    /// <param name="styles">A tuple of the style value and if should be shown.</param>
    /// <returns></returns>
    public static string StyleWhen(params (string styleName, string value, bool show)[] styles)
    {
        if(styles == null || styles.Length == 0) 
            return string.Empty;

        int length = styles
            .Where(c => c.show == true
                && string.IsNullOrWhiteSpace(c.styleName) == false
                && string.IsNullOrWhiteSpace(c.value) == false)
            //+2 for : and semicolon
            .Select(s => s.styleName.Length + 2 + s.value.Length)
            .Sum();
        
        if (length == 0) return String.Empty;

        // Using Span<T> for stack allocation and avoiding heap allocations
        Span<char> buffer = stackalloc char[length];
        int offset = 0;
        
        foreach ((string styleName, string value, bool show) in styles)
        {
            if (show == false 
                || string.IsNullOrWhiteSpace(styleName) == true
                || string.IsNullOrWhiteSpace(value) == true)
                continue;

            //add the style attribute
            styleName.AsSpan().CopyTo(buffer.Slice(offset));
            offset += styleName.Length;

            buffer[offset++] = ':';

            value.AsSpan().CopyTo(buffer.Slice(offset));
            offset += value.Length;

            buffer[offset++] = ';';
        }

        if (offset > 0)
            return new string(buffer.Slice(0, offset));

        return string.Empty;
    }

}
