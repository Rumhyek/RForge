namespace RForge.Abstractions.DropDowns;

/// <summary>
/// The position of the element.
/// </summary>
public enum RfPosition
{
    /// <summary>
    /// Does not set a position leaving it as is.
    /// </summary>
    Unset,
    /// <summary>
    /// Position at the top right. Requires parent to be relative.
    /// </summary>
    TopRight,
    /// <summary>
    /// Position at the top left. Requires parent to be relative.
    /// </summary>
    TopLeft,
    /// <summary>
    /// Position at the bottom right. Requires parent to be relative.
    /// </summary>
    BottomRight,
    /// <summary>
    /// Position at the bottom left. Requires parent to be relative.
    /// </summary>
    BottomLeft,
}
