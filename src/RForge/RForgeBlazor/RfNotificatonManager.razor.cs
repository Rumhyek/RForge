using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;
using RForgeBlazor.Models;

namespace RForgeBlazor;
public partial class RfNotificatonManager : IDisposable
{
    [Inject]
    private INotificationManager _notificationManager { get; set; }

    #region Parameters
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public NotificationOptions DefaultErrorOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Danger,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
    };

    [Parameter]
    public NotificationOptions DefaultWarningOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Warning,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
    };

    [Parameter]
    public NotificationOptions DefaultInfoOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Info,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        ShowFor = 10000,
    };

    [Parameter]
    public NotificationOptions DefaultSuccessOptions { get; set; } = new NotificationOptions()
    {
        Color = RfNotificationColor.Success,
        Position = RfNotificationPosition.TopCenter,
        ShowCloseButton = true,
        ShowFor = 4000,
    };
    #endregion

    private Dictionary<RfNotificationPosition, List<(RenderFragment DisplayFragment, NotificationOptions Options)>> Messages { get; set; }

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
        Messages[baseOptions.Position].Add((content, defaultOptions));

        StateHasChanged();
    }

    private string LocationCssClass(RfNotificationPosition location)
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

    private string ColorCssClass(RfNotificationColor color)
    {
        switch (color)
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


    public void Dispose()
    {
        if (_notificationManager != null)
        {
            _notificationManager.OnShow -= NotificationManager_OnShow;

        }

        Messages.Clear();
    }
}
