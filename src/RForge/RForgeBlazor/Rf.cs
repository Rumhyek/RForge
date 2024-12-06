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
        {
            return string.Empty;
        }

        int length = classes
            .Where(c => c.show == true)
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

}
