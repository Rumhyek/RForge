using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;

namespace RForgeBlazor.Services;

public interface INotificationManager : INotificationManagerBackend
{

    /// <summary>
    /// A event that will be invoked to clear all toasts
    /// </summary>
    event Action OnClearAll;

    /// <summary>
    /// A event that will be invoked to clear toast of specified level
    /// </summary>
    event Action<RfNotificationSeverity> OnClearBySeverity;

    /// <summary>
    /// A event that will be invoked to clear toast of specified position.
    /// </summary>
    event Action<RfNotificationPosition> OnClearByPosition;

    /// <summary>
    /// A event that will be invoked when showing a notification.
    /// </summary>
    event Action<RenderFragment, NotificationOptions, Action<NotificationOptions>> OnShow;

    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddInfo(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddSuccess(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddWarning(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddError(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a notification using the supplied options
    /// </summary>
    /// <param name="content">RenderFragment to display inside the notification</param>
    /// <param name="options">Options to configure the notification instance. If null the system will use the default options.</param>
    void AddNotification(RenderFragment content, NotificationOptions options);


    /// <summary>
    /// Removes all notifications
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Removes all notifications with a specified <paramref name="RfNotificationSeverity"/>.
    /// </summary>
    void ClearAllBySeverity(RfNotificationSeverity severity);

    /// <summary>
    /// Removes all notifications with a specified <paramref name="RfNotificationPosition"/>.
    /// </summary>
    void ClearAllByPostion(RfNotificationPosition position);

    /// <summary>
    /// Removes all notifications with notification level warning
    /// </summary>
    void ClearAllWarning();

    /// <summary>
    /// Removes all notifications with notification level Info
    /// </summary>
    void ClearAllInfo();

    /// <summary>
    /// Removes all notifications with notification level Success
    /// </summary>
    void ClearAllSuccess();

    /// <summary>
    /// Removes all notifications with notification level Error
    /// </summary>
    void ClearAllError();
}
