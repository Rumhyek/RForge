using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Modal;
using RForgeBlazor.Models;

namespace RForgeBlazor.Services;
public interface IDialogManager : IDialogManagerBackend
{
    void Confirm(RfDialogOptionConfirm options);

    void Confirm(RenderFragment message, Func<bool, Task> onConfirm, string confirmText = "Ok", string cancelText = "Cancel");

    void Alert(RfDialogOptionAlert options);
    void Alert(RenderFragment message, Func<Task> onAlert, string buttonText = "Ok");
    void Alert(RenderFragment message, string buttonText = "Ok");

    void Prompt(RfDialogOptionPrompt options);
    void Prompt(RenderFragment message, Func<string, Task> onPrompt, string confirmText = "Ok", string cancelText = "Cancel");

    void RegisterDm(RfDialogManager dialogManager);
    void Unregister();
}
