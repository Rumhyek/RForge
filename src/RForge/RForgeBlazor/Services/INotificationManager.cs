using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;

namespace RForgeBlazor.Services;

/// <summary>
/// A service that manages notifications in a blazor application.
/// </summary>
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
    /// A event that will be invoked to clear toast with a given id.
    /// </summary>
    event Action<Guid> OnClearById;

    /// <summary>
    /// A event that will be invoked when showing a notification.
    /// </summary>
    event Action<RenderFragment, NotificationOptions, Action<NotificationOptions>> OnShow;

    /// <summary>
    /// Called everytime a notification is dissmissed via time or click.
    /// </summary>
    event Action<NotificationOptions> OnNotificationDimiss;

    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddInfo(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddSuccess(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddWarning(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddError(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a notification using the supplied options
    /// </summary>
    /// <param name="content">RenderFragment to display inside the notification</param>
    /// <param name="options">Options to configure the notification instance. If null the system will use the default options.</param>
    Guid AddNotification(RenderFragment content, NotificationOptions options);

    /// <summary>
    /// Shows a notification using the supplied options
    /// </summary>
    /// <param name="content">RenderFragment to display inside the notification</param>
    /// <param name="options">Options to configure the notification instance.</param>
    Guid AddNotification(RenderFragment content, Action<NotificationOptions> options);


    /// <summary>
    /// Removes all notifications
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Removes all notifications with a specified <paramref name="severity"/>.
    /// </summary>
    void ClearAllBySeverity(RfNotificationSeverity severity);

    /// <summary>
    /// Removes all notifications with a specified <paramref name="position"/>.
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

    /// <summary>
    /// Removes all notifications with given ids
    /// </summary>
    /// <param name="id"></param>
    void ClearById(params Guid[] id);

    /// <summary>
    /// Called internally to handle frontend removing a notification
    /// </summary>
    /// <param name="notification"></param>
    internal void NotifyNotificationRemoval(NotificationOptions notification);
}
