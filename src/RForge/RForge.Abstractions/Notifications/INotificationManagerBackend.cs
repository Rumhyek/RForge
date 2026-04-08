namespace RForge.Abstractions.Notifications;

/// <summary>
/// A way for Interactive Server blazor apps to communicate with the frontend to push notifications.
/// </summary>
public interface INotificationManagerBackend
{
    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddInfo(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddSuccess(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddWarning(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddError(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddInfo(string message, string title, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddSuccess(string message, string title, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddWarning(string message, string title, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    Guid AddError(string message, string title, Action<NotificationOptions> options = null);
}

