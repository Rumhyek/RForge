using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;
using System.Runtime.InteropServices;

namespace RForgeBlazor.Services;
public class DialogManager : IDialogManager
{
    private RfDialogManager _dialogManager;

    private Queue<RfDialogOption> _dialogOptions = new();

    public void Alert(RfDialogOptionAlert options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }

        _dialogManager.Show(options);
    }

    public void Prompt(RfDialogOptionPrompt options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }

        _dialogManager.Show(options);
    }

    public void Confirm(RfDialogOptionConfirm options)
    {
        if (_dialogManager == null)
        {
            _dialogOptions.Enqueue(options);
            return;
        }

        _dialogManager.Show(options);
    }

    #region Alert Overloads

    public void Alert(RenderFragment message, Func<Task> onAlert, string buttonText = "Ok")
    {
        Alert(new RfDialogOptionAlert()
        {
            Message = message,
            OnAlert = onAlert,
            ConfirmText = buttonText,
        });
    }

    public void Alert(string message, Func<Task> onAlert)
    {
        Alert(RfDialogManager.MessageOnly(message), onAlert);
    }

    public void Alert(string message, string title, Func<Task> onAlert, string buttonText = "Ok")
    {
        Alert(RfDialogManager.MessageAndTitle((message, title)), onAlert, buttonText);
    }

    private readonly Func<Task> emptyAlert = () => Task.CompletedTask;
    public void Alert(string message, string title, string buttonText = "Ok")
    {
        Alert(message, title, emptyAlert, buttonText);
    }

    public void Alert(string message)
    {
        Alert(message, emptyAlert);
    }
    public void Alert(RenderFragment message, string buttonText = "Ok")
    {
        Alert(message, emptyAlert, buttonText);
    }
    #endregion

    #region Confirm Overloads

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

    public void Confirm(string message, Func<bool, Task> onConfirm)
    {
        Confirm(RfDialogManager.MessageOnly(message), onConfirm);
    }

    public void Confirm(string message, string title, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel")
    {
        Confirm(RfDialogManager.MessageAndTitle((message, title)), onConfirm, confirmText, cancelText);
    }

    #endregion

    #region Prompt Overloads
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

    public void Prompt(string message, Func<string, Task> onPrompt)
    {
        Prompt(RfDialogManager.MessageOnly(message), onPrompt);
    }

    public void Prompt(string message, string title, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel")
    {
        Prompt(RfDialogManager.MessageAndTitle((message, title)), onPrompt, confirmText, cancelText);
    } 
    #endregion

    public void RegisterDm(RfDialogManager dialogManager)
    {
        _dialogManager = dialogManager;
        while(_dialogOptions.Count > 0)
        {
            var dialog = _dialogOptions.Dequeue();
            _dialogManager.Show(dialog);
        }
    }

    public void Unregister()
    {
        _dialogManager = null;
    }
}
