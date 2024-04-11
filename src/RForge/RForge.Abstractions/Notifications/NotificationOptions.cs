using System.ComponentModel;

namespace RForge.Abstractions.Notifications;

/// <summary>
/// Configuration for a notification.
/// </summary>
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

    /// <summary>
    /// The color to set the notification. Uses Bulma theme files.
    /// </summary>
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

