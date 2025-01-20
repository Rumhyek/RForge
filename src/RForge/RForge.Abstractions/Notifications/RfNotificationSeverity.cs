namespace RForge.Abstractions.Notifications;

/// <summary>
/// Used to set the severity of a notification. This will set the default style / behaviour of a notfication.
/// </summary>
public enum RfNotificationSeverity
{
    /// <summary>
    /// Informational notification.
    /// </summary>
    Info,
    /// <summary>
    /// Successful operation notification.
    /// </summary>
    Success,
    /// <summary>
    /// Warning notification.
    /// </summary>
    Warning,
    /// <summary>
    /// Error notification.
    /// </summary>
    Error,
}

