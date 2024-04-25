using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;
using static RForgeBlazor.RfNotificationManager;

namespace RForgeBlazor;

public partial class RfNotificationManagerNotification
{
    #region Parameters
    [CascadingParameter]
    ///<summary>
    /// Required to communicate removing from the notification manager.
    ///</summary>
    public RfNotificationManager NotificatonManager { get; set; }

    [Parameter]
    ///<summary>
    /// The detail object that repersents this notification.
    ///</summary>
    public NotificationDetails Details { get; set; }
    #endregion

    private async void OnCloseClick()
    {
        NotificatonManager.OnRemoveNotification(Details);
    }

    private void OnTimerElapsed()
    {
        NotificatonManager.OnRemoveNotification(Details);
    }

    #region Computeds
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