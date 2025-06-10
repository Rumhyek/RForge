using RForge.Abstractions;
using RForge.Abstractions.Modal;

namespace RForgeBlazor.Models;

/// <summary>
/// Represents a dialog that allows the user to select from multiple actions.
/// </summary>
/// <remarks>This class extends <see cref="RfDialogOption"/> to provide functionality for dialogs where the user
/// can choose from a predefined set of actions. The selected action can trigger a callback function.</remarks>
public class RfDialogOptionMultiAction : RfDialogOption
{
    /// <summary>
    /// Gets the type of the dialog.
    /// </summary>
    public override RfDialogType DialogType => RfDialogType.MulitAction;
    /// <summary>
    /// Gets or sets the callback function to invoke when an action is selected.
    /// </summary>
    public Func<RfDialogMultiActionButtonOptions, Task> OnAction { get; set; }
    /// <summary>
    /// Gets or sets the actions available for the user to choose from.
    /// </summary>
    public RfDialogMultiActionButtonOptions[] Actions { get; set; }
}
