using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;
using static RForgeBlazor.RfNotificationManager;

namespace RForgeBlazor;

/// <summary>
/// Represents a notification within the RfNotificationManager.
/// </summary>
public partial class RfNotificationManagerNotification
{
    #region Parameters
    ///<summary>
    /// Required to communicate removing from the notification manager.
    ///</summary>
    [CascadingParameter]
    public RfNotificationManager NotificatonManager { get; set; }

    ///<summary>
    /// The detail object that represents this notification.
    ///</summary>
    [Parameter]
    public NotificationDetails Details { get; set; }
    #endregion

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private void OnCloseClick()
    {
        NotificatonManager.OnRemoveNotification(Details);
    }

    /// <summary>
    /// Handles the timer elapsed event.
    /// </summary>
    private void OnTimerElapsed()
    {
        NotificatonManager.OnRemoveNotification(Details);
    }

    #region Computeds
    /// <summary>
    /// Gets the CSS class for the notification color based on the notification details.
    /// </summary>
    private string ColorCssClass
    {
        get
        {
            if (Details == null)
                return null;

            switch (Details.Options.Color)
            {
                case RfNotificationColor.Danger: return "is-danger";
                case RfNotificationColor.Primary: return "is-primary";
                case RfNotificationColor.Link: return "is-link";
                case RfNotificationColor.Info: return "is-info";
                case RfNotificationColor.Success: return "is-success";
                case RfNotificationColor.Warning: return "is-warning";
            }

            return null;
        }
    }
    #endregion
}
