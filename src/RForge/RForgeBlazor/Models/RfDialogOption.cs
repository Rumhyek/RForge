using Microsoft.AspNetCore.Components;
using RForge.Abstractions;

namespace RForgeBlazor.Models;

public abstract class RfDialogOption
{
    public RenderFragment Message { get; set; }
    public string ConfirmText { get; set; }

    public abstract RfDialogType DialogType { get; }

}
