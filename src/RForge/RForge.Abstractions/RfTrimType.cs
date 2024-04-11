namespace RForge.Abstractions;

/// <summary>
/// The method in how to trim a string.
/// </summary>
public enum RfTrimType
{
    /// <summary>
    /// The string will not be trimmed.
    /// </summary>
    None,
    /// <summary>
    /// The string will be trimmed on both the start and end.
    /// </summary>
    TrimBoth,
    /// <summary>
    /// The string will be trimmed only from the start.
    /// </summary>
    TrimStart,
    /// <summary>
    /// The string will be trimmed only from the end.
    /// </summary>
    TrimEnd
}
