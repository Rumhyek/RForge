using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;

namespace RForgeBlazor;
/// <summary>
/// Represents a context menu component making use of Bulma dropdown component.
/// </summary>
public partial class RfContextMenu
{
    /// <summary>
    /// Gets or sets a value indicating whether the context menu is active.
    /// </summary>
    [Parameter]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu allows toggling its active state on click. Default is true. 
    /// You can still manually set the IsActive parameter to control the state programmatically.
    /// </summary>
    [Parameter]
    public bool AllowIsActiveToggleClick { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback that is invoked when the IsActive property changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsActiveChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu is hoverable.
    /// </summary>
    [Parameter]
    public bool IsHoverable { get; set; } = true;

    /// <summary>
    /// Gets or sets the position of the context menu.
    /// </summary>
    [Parameter]
    public RfPosition Position { get; set; } = RfPosition.Unset;

    /// <summary>
    /// Gets or sets the CSS class for the context menu.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for the trigger button.
    /// </summary>
    [Parameter]
    public string TriggerButtonCss { get; set; }

    /// <summary>
    /// Gets or sets the position of the dropdown.
    /// </summary>
    [Parameter]
    public RfDropDownPosition DropDownPosition { get; set; } = RfDropDownPosition.LeftDown;

    /// <summary>
    /// Gets or sets the icon for the dropdown.
    /// </summary>
    [Parameter]
    public string DropDownIcon { get; set; } = "fa-solid fa-ellipsis";
    
    /// <summary>
    /// Gets or sets the CSS class for the trigger button when the context menu is active.
    /// </summary>
    [Parameter]
    public string IsActiveTriggerButtonCss { get; set; } = "is-primary";

    /// <summary>
    /// Gets or sets the text for the trigger button.
    /// </summary>
    [Parameter]
    public string TriggerText { get; set; }

    /// <summary>
    /// Gets or sets the child content to be rendered inside the context menu.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private int DropDownId { get; set; } = new Random().Next(100000, 999999);

    private string DropDownDirectionCss
    {
        get
        {
            return DropDownPosition switch
            {
                RfDropDownPosition.LeftDown => null,
                RfDropDownPosition.RightDown => "is-right",
                RfDropDownPosition.LeftUp => "is-up",
                RfDropDownPosition.RightUp => "is-up is-right",
                _ => null
            };
        }
    }

    private string PositionCss
    {
        get
        {
            return Position switch
            {
                RfPosition.Unset => null,
                RfPosition.TopRight => "is-top-right",
                RfPosition.TopLeft => "is-top-left",
                RfPosition.BottomRight => "is-bottom-right",
                RfPosition.BottomLeft => "is-bottom-left",
                _ => null
            };
        }
    }

    private async Task ToggleIsActiveClick()
    {
        if (AllowIsActiveToggleClick == false) return;
        IsActive = IsActive == false;
        await IsActiveChanged.InvokeAsync(IsActive);
    }
}
