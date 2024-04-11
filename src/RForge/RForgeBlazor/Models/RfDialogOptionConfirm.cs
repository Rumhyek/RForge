using RForge.Abstractions;
namespace RForgeBlazor.Models;

public class RfDialogOptionConfirm : RfDialogOption
{
    public override RfDialogType DialogType => RfDialogType.Confirm;
    public string CancelText { get; set; }
    public Func<bool, Task> OnConfirm { get; set; }
}
