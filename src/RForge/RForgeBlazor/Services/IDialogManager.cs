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
    /// Displays a message and executes one of multiple actions based on user input.
    /// </summary>
    /// <remarks>This method allows the caller to define multiple actions and handle user selection
    /// dynamically.  Ensure that <paramref name="actions"/> contains valid identifiers, as they will be used to
    /// determine the available options.</remarks>
    /// <param name="message">The content to render as the message displayed to the user.</param>
    /// <param name="onAction">A callback function that is invoked when an action is selected. The function receives the selected action's
    /// options.</param>
    /// <param name="actions">An array of action identifiers representing the available actions. Must not be null or empty.</param>
    void MultiAction(RenderFragment message, Func<RfDialogMultiActionButtonOptions, Task> onAction, string[] actions);

    /// <summary>
    /// Displays a dialog with multiple action buttons and a custom message.
    /// </summary>
    /// <remarks>Use this method to create a dialog with multiple actionable buttons, allowing users to choose
    /// from predefined options.  Ensure that the <paramref name="actions"/> array is not null or empty, and that each
    /// option is properly configured.</remarks>
    /// <param name="message">The content to render as the message displayed to the user.</param>
    /// <param name="onAction">A callback function that is invoked when an action is selected. The function receives the selected action's
    /// options.</param>
    /// <param name="actions">An array of action button options to display in the dialog. Each option defines the behavior and appearance of a
    /// button.</param>
    void MultiAction(RenderFragment message, Func<RfDialogMultiActionButtonOptions, Task> onAction, RfDialogMultiActionButtonOptions[] actions);

    /// <summary>
    /// Shows a multi-action dialog with the specified options.
    /// </summary>
    /// <remarks>Use this method to give the user multiple option buttons.</remarks>
    /// <param name="options">The options for the multi-action dialog.</param>
    void MultiAction(RfDialogOptionMultiAction options);

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
