namespace RForgeBlazor.Models;

public class RfDialogOptionPrompt : RfDialogOption
{
    public override RfDialogType DialogType => RfDialogType.Prompt;
    public string CancelText { get; set; }
    public Func<string, Task> OnPrompt { get; set; }
}