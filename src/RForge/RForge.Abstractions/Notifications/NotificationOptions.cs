using System.ComponentModel;

namespace RForge.Abstractions.Notifications;

public enum RfNotificationColor
{
    Default,
    Primary,
    Link,
    Info,
    Success,
    Warning,
    Danger
}

public enum RfNotificationPosition
{
    TopCenter,
    TopRight,
    TopLeft,
    BottomCenter,
    BottomRight,
    BottomLeft
}

public enum RfNotificationSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

public class NotificationOptions
{
    /// <summary>
    /// The <c>CssClass</c> property is used to specify additional CSS classes that will be applied to the notification. 
    /// </summary>
    public string CssClass { get; set; }

    /// <summary>
    /// The possible values for the <c>Icon</c> property are names of icons from the FontAwesome and Material icon libraries. 
    /// By providing the name of the desired icon, the corresponding icon will be displayed on the notification.
    /// </summary>
    public string Icon { get; set; }

    public RfNotificationColor Color { get; set; }

    /// <summary>
    /// Used to help route notifications details. By default <c>RfNotificationSeverity.Info</c>
    /// </summary>
    public RfNotificationSeverity Severity { get; set; } = RfNotificationSeverity.Info;

    /// <summary>
    /// Setting this property will override the global toast position property and allows you to set a specific position for this notification. 
    /// </summary>
    public RfNotificationPosition Position { get; set; }

    /// <summary>
    /// The <c>ShowFor</c> property determines the amount of time, in miliseconds, that the notification will be displayed before it is automatically closed.
    /// If left null the notification will not auto close.
    /// </summary>
    public int? ShowFor { get; set; }

    /// <summary>
    /// If true the close button will be shown. Note: ShowCloseButton will be set to true if ShowFor = null.
    /// </summary>
    public bool ShowCloseButton { get; set; }


}

//public class ToastSettings
//{
//    /// <summary>
//    /// The <c>IconType</c> property determines the type of icon that will be displayed on the toast notification. 
//    /// This property is an optional feature that can be used to provide users with additional visual cues about the notification.
//    /// </summary>
//    public IconType? IconType { get; set; }

//    /// <summary>
//    /// Enabling the <c>ShowProgressBar</c> property provides visual feedback on the remaining time for the toast notification 
//    /// based on the <c>Timeout</c> property. 
//    /// </summary>    
//    public bool ShowProgressBar { get; set; }

//    /// <summary>
//    /// When the <c>PauseProgressOnHover</c> property is enabled, the timeout period for the toast notification will be paused when the user hovers the mouse over the toast.
//    /// </summary>    
//    /// <remarks>
//    /// This can be useful for providing users with more time to read the contents of the notification. By using the <c>PauseProgressOnHover</c> property in
//    /// conjunction with the <c>ExtendedTimeout</c> property, you can create a toast notification that is more user-friendly and provides better visual feedback to the user.
//    /// </remarks>
//    public bool PauseProgressOnHover { get; set; }

//    /// <summary>
//    /// The ShowCloseButton property determines whether or not the close button is displayed on the toast notification.
//    /// </summary>
//    public bool ShowCloseButton { get; set; }


//    /// <summary>
//    /// Setting this property will override the global toast position property and allows you to set a specific position for this toast notification. 
//    /// The position can be set to one of the predefined values in the <c>ToastPosition</c> enumeration.
//    /// </summary>
//    public RfNotificationPosition Position { get; set; }

//    internal string PositionClass => $"position-{Position.ToString().ToLower()}";

//    public ToastSettings(
//        string additionalClasses,
//        string icon,
//        bool showProgressBar,
//        bool showCloseButton,
//        Action? onClick,
//        int timeout,
//        bool disableTimeout,
//        bool pauseProgressOnHover,
//        int extendedTimeout,
//        ToastPosition? toastPosition)
//    {
//        AdditionalClasses = additionalClasses;
//        IconType = iconType;
//        Icon = icon;
//        ShowProgressBar = showProgressBar;
//        ShowCloseButton = showCloseButton;
//        OnClick = onClick;
//        Timeout = timeout;
//        DisableTimeout = disableTimeout;
//        PauseProgressOnHover = pauseProgressOnHover;
//        ExtendedTimeout = extendedTimeout;
//        Position = toastPosition;

//        if (onClick is not null)
//        {
//            AdditionalClasses += " blazored-toast-action";
//        }
//    }

//}


public interface INotificationManagerBackend
{
    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddInfo(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddSuccess(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddWarning(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddError(string message, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddInfo(string message, string title, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddSuccess(string message, string title, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddWarning(string message, string title, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="message">Text to display on the notification</param>
    /// <param name="title">Text to display above the messsage on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddError(string message, string title, Action<NotificationOptions> options = null);
}

