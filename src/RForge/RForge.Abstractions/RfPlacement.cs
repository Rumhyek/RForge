namespace RForge.Abstractions;

/// <summary>
/// Specifies the placement options for aligning an element within its container. Used by components like the <see cref="RForge.Abstractions.Modal.RfDialogMultiActionButtonOptions"/> to determine how buttons or other elements should be positioned.
/// </summary>
public enum RfPlacement
{
    /// <summary>
    /// Aligns the element to the left side of its container.
    /// </summary>
    Left = 1,
    /// <summary>
    /// Aligns the element to the center of its container.
    /// </summary>
    Center = 2,
    /// <summary>
    /// Aligns the element to the right side of its container.
    /// </summary>
    Right = 3,
}