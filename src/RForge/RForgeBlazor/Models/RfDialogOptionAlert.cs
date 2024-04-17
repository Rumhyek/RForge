using RForge.Abstractions;

namespace RForgeBlazor.Models;

public class RfDialogOptionAlert : RfDialogOption
{
    public override RfDialogType DialogType => RfDialogType.Alert;
    public Func<Task> OnAlert { get; set; }
}
