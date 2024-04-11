using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RForgeBlazor.Models;
using RForgeBlazor.Services;
using System.ComponentModel.DataAnnotations;

namespace RForgeBlazor;
/// <summary>
/// Needed to render notifcations from <see cref="IDialogManager"/> or <see cref="IDialogManagerBackend"/>.
/// </summary>
/// <example>
/// <code>
/// &lt;RfDialogManager  /&gt;
/// </code>
/// </example>
public partial class RfDialogManager : IDisposable
{
    [Inject]
    private IDialogManager DialogManager { get; set; }

    private Queue<RfDialogOption> PendingDialogs { get; set; } = new Queue<RfDialogOption>();
    private RfDialogOption ActiveDialog { get; set; }

    private EditContext EditContext { get; set; }
    private PromptDialogForm PromptFormData { get; set; } = new();
    private bool IsLoading { get; set; }

    protected override void OnInitialized()
    {
        DialogManager.RegisterDm(this);

        EditContext = new EditContext(PromptFormData);
        EditContext.SetFieldCssClassProvider(new CustomFieldClassProvider());
    }

    /// <summary>
    /// Call to show a dialog message.
    /// </summary>
    /// <param name="options">The options for the dialog including its type.</param>
    public void Show(RfDialogOption options)
    {
        if (ActiveDialog != null)
        {
            PendingDialogs.Enqueue(options);
        }
        else
        {
            ActiveDialog = options;
            StateHasChanged();
        }
    }

    private async Task OnConfirmClick()
    {
        if (IsLoading == true) return;

        IsLoading = true;
        StateHasChanged();
        try
        {
            switch (ActiveDialog.DialogType)
            {
                case RfDialogType.Alert:
                    await OnAlertTypeConfirm(ActiveDialog as RfDialogOptionAlert);
                    break;
                case RfDialogType.Confirm:
                    await OnConfirmTypeConfirm(ActiveDialog as RfDialogOptionConfirm);
                    break;
                case RfDialogType.Prompt:
                    await OnPromptTypeConfirm(ActiveDialog as RfDialogOptionPrompt);
                    break;
            }
        }
        catch
        {

        }

        if (PendingDialogs.Count > 0)
            ActiveDialog = PendingDialogs.Dequeue();
        else
            ActiveDialog = null;

        IsLoading = false;

        StateHasChanged();
    }

    private async Task OnCancelClick()
    {
        if (IsLoading == true) return;
        IsLoading = true;
        StateHasChanged();
        try
        {
            switch (ActiveDialog.DialogType)
            {
                case RfDialogType.Alert:
                    await OnAlertTypeCancel(ActiveDialog as RfDialogOptionAlert);
                    break;
                case RfDialogType.Confirm:
                    await OnConfirmTypeCancel(ActiveDialog as RfDialogOptionConfirm);
                    break;
                case RfDialogType.Prompt:
                    await OnPromptTypeCancel(ActiveDialog as RfDialogOptionPrompt);
                    break;
            }
        }
        catch
        {

        }

        if (PendingDialogs.Count > 0)
            ActiveDialog = PendingDialogs.Dequeue();
        else
            ActiveDialog = null;

        IsLoading = false;

        StateHasChanged();
    }

    private async Task OnAlertTypeConfirm(RfDialogOptionAlert options) => await options.OnAlert();
    private Task OnAlertTypeCancel(RfDialogOptionAlert options) => Task.CompletedTask;

    private async Task OnConfirmTypeConfirm(RfDialogOptionConfirm options) => await options.OnConfirm(true);
    private async Task OnConfirmTypeCancel(RfDialogOptionConfirm options) => await options.OnConfirm(false);

    private async Task OnPromptTypeConfirm(RfDialogOptionPrompt options)
    {
        await options.OnPrompt(PromptFormData.Input);
        PromptFormData = new PromptDialogForm();
        EditContext = new EditContext(PromptFormData);
        EditContext.SetFieldCssClassProvider(new CustomFieldClassProvider());
    }
    private async Task OnPromptTypeCancel(RfDialogOptionPrompt options)
    {
        await options.OnPrompt(null);
        PromptFormData = new PromptDialogForm();
        EditContext = new EditContext(PromptFormData);
        EditContext.SetFieldCssClassProvider(new CustomFieldClassProvider());
    }

    public void Dispose()
    {
        DialogManager.Unregister();
    }

    #region Computeds
    private string IsLoadingCss
    {
        get
        {
            if (IsLoading == true) return "is-loading";

            return null;
        }
    }

    #endregion

    private class PromptDialogForm
    {
        [Required]
        public string Input { get; set; }
    }

    private class CustomFieldClassProvider : FieldCssClassProvider
    {
        public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
        {

            var isValid = editContext.GetValidationMessages(fieldIdentifier).Any() == false;

            if (editContext.IsModified(fieldIdentifier) == true)
            {
                if (isValid == true)
                    return "modified valid";

                return "modified invalid";
            }

            if (isValid == true)
                return "valid";

            return "invalid";
        }
    }
}
