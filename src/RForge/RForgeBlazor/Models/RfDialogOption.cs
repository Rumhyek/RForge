using Microsoft.AspNetCore.Components;
using RForge.Abstractions;

namespace RForgeBlazor.Models;

/// <summary>
/// Represents the base class for dialog options. Must be inherited.
/// </summary>
public abstract class RfDialogOption
{
    /// <summary>
    /// Gets or sets the message to display in the dialog.
    /// </summary>
    public RenderFragment Message { get; set; }
    /// <summary>
    /// Gets or sets the text for the confirm button.
    /// </summary>
    public string ConfirmText { get; set; }

    /// <summary>
    /// Gets the type of the dialog.
    /// </summary>
    public abstract RfDialogType DialogType { get; }

}
