using RForge.Abstractions;
namespace RForgeBlazor.Models;

/// <summary>
/// Represents the options for a confirm dialog.
/// </summary>
public class RfDialogOptionConfirm : RfDialogOption
{
    /// <summary>
    /// Gets the type of the dialog.
    /// </summary>
    public override RfDialogType DialogType => RfDialogType.Confirm;
    /// <summary>
    /// Gets or sets the text for the cancel button.
    /// </summary>
    public string CancelText { get; set; }
    /// <summary>
    /// Gets or sets the callback function to invoke when the confirm dialog is confirmed or canceled.
    /// </summary>
    public Func<bool, Task> OnConfirm { get; set; }
}
