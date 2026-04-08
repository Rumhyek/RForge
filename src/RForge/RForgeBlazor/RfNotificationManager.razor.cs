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
        _notificationManager.OnClearById += NotificationManager_OnClearById;
    }

    /// <summary>
    /// Removes notifications that satisfy the specified criteria from all notification collections.
    /// </summary>
    /// <remarks>For each notification removed, a notification removal event is triggered. This method affects
    /// all notification locations managed by the notification manager.</remarks>
    /// <param name="selector">A function that determines whether a given notification should be removed. The function receives a
    /// NotificationDetails object and returns <see langword="true"/> to remove the notification; otherwise, <see
    /// langword="false"/>.</param>
    /// <returns><see langword="true"/> if one or more notifications were removed; otherwise, <see langword="false"/>.</returns>
    private bool RemoveNotifcations(Func<NotificationDetails, bool> selector)
    {
        bool stateHasChanged = false;
        foreach (List<NotificationDetails> notificationLocations in Messages.Values)
        {
            var list = notificationLocations.Where(selector).ToList();
            stateHasChanged = stateHasChanged || list.Count > 0;

            foreach (var notification in list)
            {
                notificationLocations.Remove(notification);
                _notificationManager.NotifyNotificationRemoval(notification.Options);
                notification.Dispose();
            }
        }

        if (stateHasChanged == true)
            StateHasChanged();

        return stateHasChanged;
    }
    /// <summary>
    /// Removes notifications associated with the specified notification identifier and updates the component state if
    /// any notifications were removed.
    /// </summary>
    private void NotificationManager_OnClearById(Guid id) => RemoveNotifcations(n => n.Options.NotificationId == id);

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnClearBySeverity"/>. Clears notifications by severity.
    /// </summary>
    /// <param name="severity">The severity of the notifications to clear.</param>
    private void NotificationManager_OnClearBySeverity(RfNotificationSeverity severity) => RemoveNotifcations(n => n.Options.Severity == severity);

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnClearByPosition"/>. Clears notifications by position.
    /// </summary>
    /// <param name="position">The position of the notifications to clear.</param>
    private void NotificationManager_OnClearByPosition(RfNotificationPosition position) => RemoveNotifcations(n => n.Options.Position == position);

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnClearAll"/>. Clears all notifications.
    /// </summary>
    private void NotificationManager_OnClearAll() => RemoveNotifcations(n => true);

    /// <summary>
    /// Removes a notification.
    /// </summary>
    /// <param name="notification">The notification to remove.</param>
    internal void OnRemoveNotification(NotificationDetails notification) => RemoveNotifcations(n => n.Id == notification.Id);

    /// <summary>
    /// Listens to <see cref="NotificationManager.OnShow"/>. Shows a notification.
    /// </summary>
    /// <param name="content">The content to display in the notification.</param>
    /// <param name="baseOptions">The base options for the notification.</param>
    /// <param name="configuration">The configuration action for the notification options.</param>
    private void NotificationManager_OnShow(RenderFragment content, NotificationOptions baseOptions, Action<NotificationOptions> configuration)
    {
        baseOptions ??= new NotificationOptions()
        {
            NotificationId = Guid.NewGuid()
        };

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
            Id = baseOptions.NotificationId,
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
            _notificationManager.OnClearById -= NotificationManager_OnClearById;
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
    public class NotificationDetails : IDisposable
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

        /// <summary>
        /// Disposes of the timer if any
        /// </summary>
        public void Dispose()
        {
            Timer?.Dispose();
            Timer = null;
        }
    }
}
