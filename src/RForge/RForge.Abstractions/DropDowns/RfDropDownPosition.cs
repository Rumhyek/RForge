namespace RForge.Abstractions.DropDowns;

/// <summary>
/// The direction to open the drop down.
/// </summary>
public enum RfDropDownPosition
{
    LeftDown,
    RightDown,
    LeftUp,
    RightUp,
}

public enum RfShowSelectionInDropDown
{
    /// <summary>
    /// Only show the selected item at the top when it is not in the list already.
    /// </summary>
    OnlyWhenNotInList,
    /// <summary>
    /// The selected item will always render at the top of the drop down even if it is already one of the options shown in the drop down.
    /// </summary>
    AlwaysAtTop,
    /// <summary>
    /// Never show the selected item at the top. Only show it when it is an option to select from.
    /// </summary>
    Never,
}