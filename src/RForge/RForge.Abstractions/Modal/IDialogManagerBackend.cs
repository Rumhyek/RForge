namespace RForge.Abstractions.Modal;
/// <summary>
/// A way for Interactive Server blazor apps to communicate with the frontend to push dialogs.
/// </summary>
public interface IDialogManagerBackend
{
    /// <summary>
    /// Request the user to confirm a statement and the results of their confirmation is passed into <paramref name="onConfirm"/> True for confirm and false for cancel.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="onConfirm">The callback function giving the users's answer.</param>
    void Confirm(string message, Func<bool, Task> onConfirm);
    /// <summary>
    /// Request the user to confirm a statement and the results of their confirmation is passed into <paramref name="onConfirm"/> True for confirm and false for cancel.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="title">A title to show above the message in the dialog box.</param>
    /// <param name="onConfirm">The callback function giving the users's answer.</param>
    /// <param name="confirmText">The text to use for confirm button.</param>
    /// <param name="cancelText">The text to use for the cancel button.</param>
    void Confirm(string message, string title, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel");

    /// <summary>
    /// Show an alert dialog to the user requiring the user to click ok to continue.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="onAlert">The callback function to call when the user clicks ok.</param>
    void Alert(string message, Func<Task> onAlert);
    /// <summary>
    /// Show an alert dialog to the user requiring the user to click ok to continue.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    void Alert(string message);
    /// <summary>
    /// Show an alert dialog to the user requiring the user to click ok to continue.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="title">A title to show above the message in the dialog box.</param>
    /// <param name="confirmText">The text to use for confirm button.</param>
    void Alert(string message, string title, string confirmText = "Ok");
    /// <summary>
    /// Show an alert dialog to the user requiring the user to click ok to continue.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="title">A title to show above the message in the dialog box.</param>
    /// <param name="onAlert"></param>
    /// <param name="confirmText">The text to use for confirm button.</param>
    void Alert(string message, string title, Func<Task> onAlert, string confirmText = "Ok");

    /// <summary>
    /// Prompts the user for a response via a textbox and the results of their prompt is passed into <paramref name="onPrompt"/>. If null the user clicked cancel.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="onPrompt">The callback function giving the users's answer. Null means they cancelled.</param>
    void Prompt(string message, Func<string, Task> onPrompt);
    /// <summary>
    /// Prompts the user for a response via a textbox and the results of their prompt is passed into <paramref name="onPrompt"/>. If null the user clicked cancel.
    /// </summary>
    /// <param name="message">The message to show in the dialog box.</param>
    /// <param name="title">A title to show above the message in the dialog box.</param>
    /// <param name="onPrompt">The callback function giving the users's answer. Null means they cancelled.</param>
    /// <param name="confirmText">The text to use for confirm button.</param>
    /// <param name="cancelText">The text to use for the cancel button.</param>
    void Prompt(string message, string title, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel");
}
