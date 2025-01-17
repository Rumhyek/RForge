using Microsoft.AspNetCore.Components;
using RForge.Abstractions;
using RForge.Abstractions.Notifications;
using RForgeBlazor.Services;

namespace RForgeBlazor;

/// <summary>
/// Needed to render notifcations from <see cref="INotificationManager"/> or <see cref="INotificationManagerBackend"/>.
/// </summary>
/// <example>
/// <code>
/// &lt;RfNotificationManager  /&gt;
/// </code>
/// </example>
public partial class RfNotificationManager : IDisposable
{
    [Inject]
    private INotificationManager _notificationManager { get; set; }

    #region Parameters
    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Error"/> notification.
    ///</summary>
    [Parameter]
    public NotificationOptions DefaultErrorOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Danger,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        Icon = "fa-solid fa-circle-minus",
    };

    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Warning"/> notification.
    ///</summary>
    [Parameter]
    public NotificationOptions DefaultWarningOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Warning,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        Icon = "fa-solid fa-circle-exclamation",
    };

    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Info"/> notification.
    ///</summary>
    [Parameter]
    public NotificationOptions DefaultInfoOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Info,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        ShowFor = 10000,
        Icon = "fa-solid fa-circle-info",
    };

    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Success"/> notification.
    ///</summary>
    [Parameter]
    public NotificationOptions DefaultSuccessOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Success,
        Position = RfNotificationPosition.BottomRight,
        ShowCloseButton = true,
        ShowFor = 4000,
        Icon = "fa-solid fa-circle-check",
    };
    #endregion

    private Dictionary<RfNotificationPosition, List<NotificationDetails>> Messages { get; set; }

    /// <summary>
    /// Initializes the component.
    /// </summary>
    protected override void OnInitialized()
    {
        Messages = new()
        {
            { RfNotificationPosition.TopLeft, new() },
            { RfNotificationPosition.TopCenter, new() },
            { RfNotificationPosition.TopRight, new() },
            { RfNotificationPosition.BottomLeft, new() },
            { RfNotificationPosition.BottomCenter, new() },
            { RfNotificationPosition.BottomRight, new() }
        };

        _notificationManager.OnShow += NotificationManager_OnShow;
        _notificationManager.OnClearAll += NotificationManager_OnClearAll;
        _notificationManager.OnClearByPosition += NotificationManager_OnClearByPosition;
        _notificationManager.OnClearBySeverity += NotificationManager_OnClearBySeverity;
    }

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnClearBySeverity"/>. Clears notifications by severity.
    /// </summary>
    /// <param name="severity">The severity of the notifications to clear.</param>
    private void NotificationManager_OnClearBySeverity(RfNotificationSeverity severity)
    {
        bool stateHasChanged = false;
        foreach (var notifications in Messages.Values)
        {
            stateHasChanged |= notifications.RemoveAll(n => n.Options.Severity == severity) > 0;
        }

        if (stateHasChanged == true)
            StateHasChanged();
    }
    /// <summary>
    /// Listens to <see cref="NotificationManager.OnClearByPosition"/>. Clears notifications by position.
    /// </summary>
    /// <param name="position">The position of the notifications to clear.</param>
    private void NotificationManager_OnClearByPosition(RfNotificationPosition position)
    {

        bool stateHasChanged = false;
        foreach (var notifications in Messages.Values)
        {
            stateHasChanged |= notifications.RemoveAll(n => n.Options.Position == position) > 0;
        }

        if (stateHasChanged == true)
            StateHasChanged();
    }

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnClearAll"/>. Clears all notifications.
    /// </summary>
    private void NotificationManager_OnClearAll()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnShow"/>. Shows a notification.
    /// </summary>
    /// <param name="content">The content to display in the notification.</param>
    /// <param name="baseOptions">The base options for the notification.</param>
    /// <param name="configuration">The configuration action for the notification options.</param>
    private void NotificationManager_OnShow(RenderFragment content, NotificationOptions baseOptions, Action<NotificationOptions> configuration)
    {
        baseOptions ??= new NotificationOptions();

        NotificationOptions defaultOptions = null;
        switch (baseOptions.Severity)
        {
            case RfNotificationSeverity.Info:
                defaultOptions = DefaultInfoOptions;
                break;
            case RfNotificationSeverity.Success:
                defaultOptions = DefaultSuccessOptions;
                break;
            case RfNotificationSeverity.Warning:
                defaultOptions = DefaultWarningOptions;
                break;
            case RfNotificationSeverity.Error:
                defaultOptions = DefaultErrorOptions;
                break;
        }

        if (defaultOptions != null)
        {
            baseOptions.CssClass = defaultOptions.CssClass;
            baseOptions.Color = defaultOptions.Color;
            baseOptions.Icon = defaultOptions.Icon;
            baseOptions.Position = defaultOptions.Position;
            baseOptions.Severity = defaultOptions.Severity;
            baseOptions.ShowCloseButton = defaultOptions.ShowCloseButton;
            baseOptions.ShowFor = defaultOptions.ShowFor;
        }

        configuration?.Invoke(baseOptions);

        var notification = new NotificationDetails()
        {
            DisplayFragment = content,
            Id = Guid.NewGuid(),
            Options = baseOptions,
        };

        if (baseOptions.ShowFor.HasValue && baseOptions.ShowFor > 0)
        {
            notification.Timer = new CountdownTimer(baseOptions.ShowFor.Value)
                .OnElapsed(() => OnRemoveNotification(notification));

            _ = notification.Timer.StartAsync();
        }

        Messages[baseOptions.Position].Add(notification);

        StateHasChanged();
    }

    /// <summary>
    /// Gets the CSS class for the notification location.
    /// </summary>
    /// <param name="location">The location of the notification.</param>
    /// <returns>The CSS class for the notification location.</returns>
    private static string LocationCssClass(RfNotificationPosition location)
    {
        switch (location)
        {
            case RfNotificationPosition.TopLeft: return "top-left";
            case RfNotificationPosition.TopCenter: return "top-center";
            case RfNotificationPosition.TopRight: return "top-right";
            case RfNotificationPosition.BottomLeft: return "bottom-left";
            case RfNotificationPosition.BottomCenter: return "bottom-center";
            case RfNotificationPosition.BottomRight: return "bottom-right";
        }


        return null;
    }

    /// <summary>
    /// Removes a notification.
    /// </summary>
    /// <param name="notification">The notification to remove.</param>
    internal void OnRemoveNotification(NotificationDetails notification)
    {
        if (notification == null) return;

        notification.Timer?.Dispose();
        notification.Timer = null;

        foreach (var location in Messages.Keys)
        {
            var notifications = Messages[location];
            if (notifications.RemoveAll(n => n.Id == notification.Id) > 0)
            {
                StateHasChanged();
                return;
            }
        }
    }

    /// <summary>
    /// Disposes the notification manager.
    /// </summary>
    public void Dispose()
    {
        if (_notificationManager != null)
        {
            _notificationManager.OnShow -= NotificationManager_OnShow;
            _notificationManager.OnClearAll -= NotificationManager_OnClearAll;
            _notificationManager.OnClearByPosition -= NotificationManager_OnClearByPosition;
            _notificationManager.OnClearBySeverity -= NotificationManager_OnClearBySeverity;
        }

        foreach (var position in Messages.Values)
        {
            foreach (var notification in position)
            {
                notification.Timer?.Dispose();
                notification.Timer = null;
            }
        }

        Messages.Clear();
    }
    /// <summary>
    /// Represents the details of a notification.
    /// </summary>
    public class NotificationDetails
    {
        /// <summary>
        /// Gets or sets the options for the notification.
        /// </summary>
        public NotificationOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the notification.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the content to display in the notification.
        /// </summary>
        public RenderFragment DisplayFragment { get; set; }

        /// <summary>
        /// Gets or sets the countdown timer for the notification.
        /// </summary>
        internal CountdownTimer Timer { get; set; }

    }
}
