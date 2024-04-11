using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DataGrids;

namespace RForgeBlazor.Models;

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
public delegate Task<bool> AsyncBoolEventHandler<TEventArgs>(object sender, TEventArgs e);
public delegate Task<string> AsyncStringEventHandler<TEventArgs>(object sender, TEventArgs e);

public class DataGridContext
{
    /// <summary>
    /// An event that is raised when a the sort changes
    /// </summary>
    public event AsyncEventHandler<DataGridSortBy> OnSortChanged;
    /// <summary>
    /// An event that is raised when a fitler changes.
    /// </summary>
    public event AsyncEventHandler<DataGridFilterBy> OnFilterChanged;


    public async Task SortChanged(DataGridSortBy sort)
    {
        if (OnSortChanged != null)
            await OnSortChanged.Invoke(this, sort);
    }

    public async Task FilterChanged(DataGridFilterBy filter)
    {
        if (OnFilterChanged != null)
            await OnFilterChanged.Invoke(this, filter);
    }
}

public abstract class RfDialogOption
{
    public RenderFragment Message { get; set; }
    public string ConfirmText { get; set; }

    public abstract RfDialogType DialogType { get; }

}


public class RfDialogOptionAlert : RfDialogOption
{
    public override RfDialogType DialogType => RfDialogType.Alert;
    public Func<Task> OnAlert { get; set; }
} 

public class RfDialogOptionConfirm : RfDialogOption
{
    public override RfDialogType DialogType => RfDialogType.Confirm;
    public string CancelText { get; set; }
    public Func<bool, Task> OnConfirm { get; set; }
}

public class RfDialogOptionPrompt : RfDialogOption
{
    public override RfDialogType DialogType => RfDialogType.Prompt;
    public string CancelText { get; set; }
    public Func<string, Task> OnPrompt { get; set; }
}