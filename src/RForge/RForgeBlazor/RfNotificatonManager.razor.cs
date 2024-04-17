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
/// &lt;RfNotificatonManager  /&gt;
/// </code>
/// </example>
public partial class RfNotificatonManager : IDisposable
{
    [Inject]
    private INotificationManager _notificationManager { get; set; }

    #region Parameters
    [Parameter]
    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Error"/> notification.
    ///</summary>
    public NotificationOptions DefaultErrorOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Danger,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        Icon = "fa-solid fa-circle-minus",
    };

    [Parameter]
    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Warning"/> notification.
    ///</summary>
    public NotificationOptions DefaultWarningOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Warning,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        Icon = "fa-solid fa-circle-exclamation",
    };

    [Parameter]
    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Info"/> notification.
    ///</summary>
    public NotificationOptions DefaultInfoOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Info,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        ShowFor = 10000,
        Icon = "fa-solid fa-circle-info",
    };

    [Parameter]
    ///<summary>
    /// The default <see cref="NotificationOptions"/> for a <see cref="RfNotificationSeverity.Success"/> notification.
    ///</summary>
    public NotificationOptions DefaultSuccessOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Success,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        ShowFor = 4000,
        Icon = "fa-solid fa-circle-check",
    };
    #endregion

    private Dictionary<RfNotificationPosition, List<NotificationDetails>> Messages { get; set; }

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

    private void NotificationManager_OnClearAll()
    {
        throw new NotImplementedException();
    }

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

    public class NotificationDetails
    {
        public NotificationOptions Options { get; set; }
        public Guid Id { get; set; }
        public RenderFragment DisplayFragment { get; set; }

        internal CountdownTimer Timer { get; set; }

    }
}
