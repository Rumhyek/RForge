using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Modal;
using RForgeBlazor.Models;

namespace RForgeBlazor.Services;

/// <inheritdoc/>
public class DialogManager : IDialogManager
{
    private RfDialogManager _dialogManager;

    private Queue<RfDialogOption> _dialogOptions = new();

    /// <inheritdoc/>
    public void Alert(RfDialogOptionAlert options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }

        _dialogManager.Show(options);
    }

    /// <inheritdoc/>
    public void Prompt(RfDialogOptionPrompt options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }

        _dialogManager.Show(options);
    }

    /// <inheritdoc/>
    public void Confirm(RfDialogOptionConfirm options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }

        _dialogManager.Show(options);
    }

    /// <inheritdoc/>
    public void MultiAction(RfDialogOptionMultiAction options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }
        _dialogManager.Show(options);
    }

    #region Alert Overloads

    /// <inheritdoc/>
    public void Alert(RenderFragment message, Func<Task> onAlert, string buttonText = "Ok")
    {
        Alert(new RfDialogOptionAlert()
        {
            Message = message,
            OnAlert = onAlert,
            ConfirmText = buttonText,
        });
    }

    /// <inheritdoc/>
    public void Alert(string message, Func<Task> onAlert)
    {
        Alert(RfDialogManager.MessageOnly(message), onAlert);
    }

    /// <inheritdoc/>
    public void Alert(string message, string title, Func<Task> onAlert, string buttonText = "Ok")
    {
        Alert(RfDialogManager.MessageAndTitle((message, title)), onAlert, buttonText);
    }

    private readonly Func<Task> emptyAlert = () => Task.CompletedTask;
    /// <inheritdoc/>
    public void Alert(string message, string title, string buttonText = "Ok")
    {
        Alert(message, title, emptyAlert, buttonText);
    }

    /// <inheritdoc/>
    public void Alert(string message)
    {
        Alert(message, emptyAlert);
    }
    /// <inheritdoc/>
    public void Alert(RenderFragment message, string buttonText = "Ok")
    {
        Alert(message, emptyAlert, buttonText);
    }
    #endregion

    #region Confirm Overloads

    /// <inheritdoc/>
    public void Confirm(RenderFragment message, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel")
    {
        Confirm(new RfDialogOptionConfirm()
        {
            CancelText = cancelText,
            ConfirmText = confirmText,
            Message = message,
            OnConfirm = onConfirm,
        });
    }

    /// <inheritdoc/>
    public void Confirm(string message, Func<bool, Task> onConfirm)
    {
        Confirm(RfDialogManager.MessageOnly(message), onConfirm);
    }

    /// <inheritdoc/>
    public void Confirm(string message, string title, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel")
    {
        Confirm(RfDialogManager.MessageAndTitle((message, title)), onConfirm, confirmText, cancelText);
    }

    #endregion

    #region Prompt Overloads
    /// <inheritdoc/>
    public void Prompt(RenderFragment message, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel")
    {
        Prompt(new RfDialogOptionPrompt()
        {
            CancelText = cancelText,
            ConfirmText = confirmText,
            Message = message,
            OnPrompt = onPrompt,
        });
    }

    /// <inheritdoc/>
    public void Prompt(string message, Func<string, Task> onPrompt)
    {
        Prompt(RfDialogManager.MessageOnly(message), onPrompt);
    }

    /// <inheritdoc/>
    public void Prompt(string message, string title, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel")
    {
        Prompt(RfDialogManager.MessageAndTitle((message, title)), onPrompt, confirmText, cancelText);
    }
    #endregion

    #region Multi Action

    /// <inheritdoc/>
    public void MultiAction(RenderFragment message, Func<RfDialogMultiActionButtonOptions, Task> onAction, RfDialogMultiActionButtonOptions[] actions)
    {
        MultiAction(new RfDialogOptionMultiAction()
        {
            Message = message,
            OnAction = onAction,
            Actions = actions
        });
    }

    /// <inheritdoc/>
    public void MultiAction(RenderFragment message, Func<RfDialogMultiActionButtonOptions, Task> onAction, string[] actions)
    {
        var actionsOptions = actions
            .Select(action => new RfDialogMultiActionButtonOptions(action))
            .ToArray();

        MultiAction(message, onAction, actionsOptions);
    }


    /// <inheritdoc/>
    public void MultiAction(string message, string title, Func<RfDialogMultiActionButtonOptions, Task> onAction, string[] actions)
    {
        var actionsOptions = actions
            .Select(action => new RfDialogMultiActionButtonOptions(action))
            .ToArray();

        MultiAction(RfDialogManager.MessageAndTitle((message, title)), onAction, actionsOptions);
    }

    /// <inheritdoc/>
    public void MultiAction(string message, Func<RfDialogMultiActionButtonOptions, Task> onAction, string[] actions)
    {
        var actionsOptions = actions
            .Select(action => new RfDialogMultiActionButtonOptions(action))
            .ToArray();

        MultiAction(RfDialogManager.MessageOnly(message), onAction, actionsOptions);
    }

    /// <inheritdoc/>
    public void MultiAction(string message, string title, Func<RfDialogMultiActionButtonOptions, Task> onAction, RfDialogMultiActionButtonOptions[] actions)
    {
        MultiAction(RfDialogManager.MessageAndTitle((message, title)), onAction, actions);
    }

    /// <inheritdoc/>
    public void MultiAction(string message, Func<RfDialogMultiActionButtonOptions, Task> onAction, RfDialogMultiActionButtonOptions[] actions)
    {
        MultiAction(RfDialogManager.MessageOnly(message), onAction, actions);
    }
    #endregion

    /// <inheritdoc/>
    public void RegisterDm(RfDialogManager dialogManager)
    {
        _dialogManager = dialogManager;
        while (_dialogOptions.Count > 0)
        {
            var dialog = _dialogOptions.Dequeue();
            _dialogManager.Show(dialog);
        }
    }

    /// <inheritdoc/>
    public void Unregister()
    {
        _dialogManager = null;
    }


}
