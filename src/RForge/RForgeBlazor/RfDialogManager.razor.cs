﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RForge.Abstractions;
using RForge.Abstractions.Modal;
using RForgeBlazor.Models;
using RForgeBlazor.Services;
using System.ComponentModel.DataAnnotations;

namespace RForgeBlazor;

/// <summary>
/// Needed to render notifcations from <see cref="IDialogManager"/> or <see cref="RForge.Abstractions.Modal.IDialogManagerBackend"/>.
/// </summary>
public partial class RfDialogManager : IDisposable
{
    /// <summary>
    /// Injected dialog manager. Used by the dialog manager to show dialogs.
    /// </summary>
    [Inject]
    private IDialogManager DialogManager { get; set; }

    /// <summary>
    /// The queue of pending dialogs to show.
    /// </summary>
    private Queue<RfDialogOption> PendingDialogs { get; set; } = new Queue<RfDialogOption>();

    /// <summary>
    /// The active dialog to show.
    /// </summary>
    private RfDialogOption ActiveDialog { get; set; }

    /// <summary>
    /// The edit context for the prompt dialog.
    /// </summary>
    private EditContext EditContext { get; set; }
    
    /// <summary>
    /// The form data for the prompt dialog.
    /// </summary>
    private PromptDialogForm PromptFormData { get; set; } = new();
    
    /// <summary>
    /// If true the dialog is loading.
    /// </summary>
    private bool IsLoading { get; set; }

    /// <summary>
    /// Initializes the component. Registers with the injected dialog manager.
    /// </summary>
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

    /// <summary>
    /// Handles the confirm button click event. Calls the appropriate method based on the dialog type.
    /// </summary>
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

    /// <summary>
    /// Handles the cancel button click event. Calls the appropriate method based on the dialog type.
    /// </summary>
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

    private async Task OnMultiActionClick( RfDialogMultiActionButtonOptions action)
    {
        if (IsLoading == true) return;
        IsLoading = true;
        
        StateHasChanged();
        
        var dialog = ActiveDialog as RfDialogOptionMultiAction;
        
        try
        {
            await dialog.OnAction(action);
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

    /// <summary>
    /// Handles the alert type confirm event. Makes use of options to call the appropriate method
    /// </summary>
    /// <param name="options"></param>
    private async Task OnAlertTypeConfirm(RfDialogOptionAlert options) => await options.OnAlert();
    /// <summary>
    /// Handles the alert type cancel event. Makes use of options to call the appropriate method
    /// </summary>
    private Task OnAlertTypeCancel(RfDialogOptionAlert options) => Task.CompletedTask;

    /// <summary>
    /// Handles the confirm type confirm event. Makes use of options to call the appropriate method
    /// </summary>
    private async Task OnConfirmTypeConfirm(RfDialogOptionConfirm options) => await options.OnConfirm(true);    
    /// <summary>
    /// Handles the confirm type cancel event. Makes use of options to call the appropriate method
    /// </summary>
    private async Task OnConfirmTypeCancel(RfDialogOptionConfirm options) => await options.OnConfirm(false);

    /// <summary>
    /// Handles the prompt type confirm event. Makes use of options to call the appropriate method
    /// </summary>
    private async Task OnPromptTypeConfirm(RfDialogOptionPrompt options)
    {
        await options.OnPrompt(PromptFormData.Input);
        PromptFormData = new PromptDialogForm();
        EditContext = new EditContext(PromptFormData);
        EditContext.SetFieldCssClassProvider(new CustomFieldClassProvider());
    }
    /// <summary>
    /// Handles the prompt type cancel event. Makes use of options to call the appropriate method
    /// </summary>
    private async Task OnPromptTypeCancel(RfDialogOptionPrompt options)
    {
        await options.OnPrompt(null);
        PromptFormData = new PromptDialogForm();
        EditContext = new EditContext(PromptFormData);
        EditContext.SetFieldCssClassProvider(new CustomFieldClassProvider());
    }

    /// <summary>
    /// Disposes the component. Unregisters with the injected dialog manager.
    /// </summary>
    public void Dispose()
    {
        DialogManager.Unregister();
    }

    #region Computeds
    /// <summary>
    /// Determines if the dialog is loading returning is-loading if it is.
    /// </summary>
    private string IsLoadingCss
    {
        get
        {
            if (IsLoading == true) return "is-loading";

            return null;
        }
    }

    private string ButtonCssByPlacement(RfPlacement placement)
    {
        return placement switch
        {
            RfPlacement.Right => "is-right",
            RfPlacement.Left => string.Empty,
            RfPlacement.Center => "flex-grow",
            _ => "is-right"
        };
    }

    #endregion

    /// <summary>
    /// Represents the form data for the prompt dialog.
    /// </summary>
    private class PromptDialogForm
    {
        /// <summary>
        /// The input value of the prompt dialog.
        /// </summary>
        [Required]
        public string Input { get; set; }
    }

    /// <summary>
    /// Custom field class provider for the dialog manager.
    /// </summary>
    private class CustomFieldClassProvider : FieldCssClassProvider
    {
        /// <summary>
        /// Gets the field css class for the field identifier.
        /// </summary>
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

    private class MultiActionLayoutData
    {
        public RfDialogOptionMultiAction Options { get; }

        public string ContainerCss { get; set; }
        public bool HasSubContainers { get; set; }
        public MultiActionLayoutData(RfDialogOptionMultiAction options)
        {
            Options = options;
        }

        public void BuildOutLayout()
        {
            bool hasLeft = Options.Actions.Any(a => a.Placement == RfPlacement.Left);
            bool hasCenter = Options.Actions.Any(a => a.Placement == RfPlacement.Center);
            bool hasRight = Options.Actions.Any(a => a.Placement == RfPlacement.Right);
          
            if(hasLeft == false && hasCenter == false && hasRight == true)
            {
                ContainerCss = "buttons is-right";
            }
            else if (hasLeft == true && hasCenter == false && hasRight == false)
            {
                ContainerCss = "buttons";
            }
            else if (hasLeft == false && hasCenter == true && hasRight == false)
            {
                ContainerCss = "buttons is-centered";
            }
            else if (hasLeft == true && hasCenter == false && hasRight == true)
            {
                ContainerCss = "is-flex is-justify-content-space-between";
                HasSubContainers = true;
            }
            else if (hasLeft == true && hasCenter == true && hasRight == false)
            {
                ContainerCss = "is-flex";
                HasSubContainers = true;
            }
            else if (hasLeft == false && hasCenter == true && hasRight == true)
            {
                ContainerCss = "is-flex";
                HasSubContainers = true;
            }
            else if (hasLeft == true && hasCenter == true && hasRight == true)
            {
                ContainerCss = "is-flex is-justify-content-space-between";
                HasSubContainers = true;
            }
            //else //if (hasLeft == false && hasCenter == false && hasRight == false)
            //{
            //    //do nothing, no actions to show
            //}
        }
    }
}
