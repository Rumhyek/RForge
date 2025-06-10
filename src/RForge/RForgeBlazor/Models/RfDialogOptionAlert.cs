using RForge.Abstractions;
using RForge.Abstractions.Modal;

namespace RForgeBlazor.Models;

/// <summary>
/// Represents the options for an alert dialog.
/// </summary>
public class RfDialogOptionAlert : RfDialogOption
{
    /// <summary>
    /// Gets the type of the dialog.
    /// </summary>
    public override RfDialogType DialogType => RfDialogType.Alert;
    /// <summary>
    /// Gets or sets the callback function to invoke when the alert is acknowledged.
    /// </summary>
    public Func<Task> OnAlert { get; set; }
}
