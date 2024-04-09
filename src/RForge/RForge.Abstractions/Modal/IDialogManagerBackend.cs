using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForge.Abstractions.Modal;
public interface IDialogManagerBackend
{
    void Confirm(string message, Func<bool, Task> onConfirm);
    void Confirm(string message, string title, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel");

    void Alert(string message, Func<Task> onAlert);
    void Alert(string message);
    void Alert(string message, string title, string buttonText = "Ok");
    void Alert(string message, string title, Func<Task> onAlert, string buttonText = "Ok");

    void Prompt(string message, Func<string, Task> onPrompt);
    void Prompt(string message, string title, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel");
}
