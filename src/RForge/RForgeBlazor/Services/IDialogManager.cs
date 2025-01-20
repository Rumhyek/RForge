using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Modal;
using RForgeBlazor.Models;

namespace RForgeBlazor.Services;

/// <summary>
/// Interface for managing dialogs in a Blazor application.
/// </summary>
public interface IDialogManager : IDialogManagerBackend
{
    /// <summary>
    /// Shows a confirm dialog with the specified options.
    /// </summary>
    /// <param name="options">The options for the confirm dialog.</param>
    void Confirm(RfDialogOptionConfirm options);

    /// <summary>
    /// Shows a confirm dialog with the specified message and callback.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="onConfirm">The callback to invoke when the dialog is confirmed or canceled.</param>
    /// <param name="confirmText">The text for the confirm button.</param>
    /// <param name="cancelText">The text for the cancel button.</param>
    void Confirm(RenderFragment message, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel");
    
    /// <summary>
    /// Shows an alert dialog with the specified options.
    /// </summary>
    /// <param name="options">The options for the alert dialog.</param>
    void Alert(RfDialogOptionAlert options);

    /// <summary>
    /// Shows an alert dialog with the specified message and callback.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="onAlert">The callback to invoke when the alert is acknowledged.</param>
    /// <param name="buttonText">The text for the alert button.</param>
    void Alert(RenderFragment message, Func<Task> onAlert, string buttonText = "Ok");
    /// <summary>
    /// Shows an alert dialog with the specified message.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="buttonText">The text for the alert button.</param>
    void Alert(RenderFragment message, string buttonText = "Ok");

    /// <summary>
    /// Shows a prompt dialog with the specified options.
    /// </summary>
    /// <param name="options">The options for the prompt dialog.</param>
    void Prompt(RfDialogOptionPrompt options);
    /// <summary>
    /// Shows a prompt dialog with the specified message and callback.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="onPrompt">The callback to invoke when the prompt is confirmed or canceled.</param>
    /// <param name="confirmText">The text for the confirm button.</param>
    /// <param name="cancelText">The text for the cancel button.</param>
    void Prompt(RenderFragment message, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel");

    /// <summary>
    /// Registers the dialog manager with the specified dialog manager instance.
    /// </summary>
    /// <param name="dialogManager">The dialog manager instance to register.</param>
    void RegisterDm(RfDialogManager dialogManager);

    /// <summary>
    /// Unregisters the dialog manager.
    /// </summary>
    void Unregister();
}
