using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;
using System.Threading;

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

    /// <summary>
    /// Process-wide counter supplying unique menu ids. A shared interlocked counter is used
    /// rather than the spec's suggested <c>Random.Shared</c>: <c>Random.Shared</c> removes the
    /// per-instance <c>Random</c> allocation but can still draw the same value twice, so it
    /// leaves the collision class open and keeps any repro test non-deterministic. A monotonic
    /// counter is collision-free by construction and yields stable ids (spec #77 design note).
    /// </summary>
    private static int _nextDropDownId;

    /// <summary>
    /// Gets the id used for this instance's dropdown menu element and the trigger's
    /// <c>aria-controls</c>. Ids are unique across every rendered instance and stable for the
    /// instance's lifetime, so <c>aria-controls</c> always resolves to exactly one menu (#77).
    /// </summary>
    private int DropDownId { get; } = Interlocked.Increment(ref _nextDropDownId);

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
