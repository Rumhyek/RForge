using RForge.Abstractions;

namespace RForgeBlazor.Models;

/// <summary>
/// Represents the options for a prompt dialog.
/// </summary>
public class RfDialogOptionPrompt : RfDialogOption
{
    /// <summary>
    /// Gets the type of the dialog.
    /// </summary>
    public override RfDialogType DialogType => RfDialogType.Prompt;
    /// <summary>
    /// Gets or sets the text for the cancel button.
    /// </summary>
    public string CancelText { get; set; }
    /// <summary>
    /// Gets or sets the callback function to invoke when the prompt is confirmed or canceled.
    /// </summary>
    public Func<string, Task> OnPrompt { get; set; }
}