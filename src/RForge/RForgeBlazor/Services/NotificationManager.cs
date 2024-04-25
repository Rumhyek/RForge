using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;

namespace RForgeBlazor.Services;

public class NotificationManager : INotificationManager
{
    public event Action OnClearAll;
    public event Action<RfNotificationSeverity> OnClearBySeverity;
    public event Action<RfNotificationPosition> OnClearByPosition;
    public event Action<RenderFragment, NotificationOptions, Action<NotificationOptions>> OnShow;

    public void ClearAll() => OnClearAll?.Invoke();

    public void ClearAllByPostion(RfNotificationPosition position) => OnClearByPosition.Invoke(position);

    public void ClearAllBySeverity(RfNotificationSeverity severity) => OnClearBySeverity.Invoke(severity);

    public void ClearAllError() => ClearAllBySeverity(RfNotificationSeverity.Error);

    public void ClearAllInfo() => ClearAllBySeverity(RfNotificationSeverity.Info);

    public void ClearAllSuccess() => ClearAllBySeverity(RfNotificationSeverity.Success);

    public void ClearAllWarning() => ClearAllBySeverity(RfNotificationSeverity.Warning);

    private void CallOnShow(RenderFragment content, NotificationOptions baseOptions, Action<NotificationOptions> options)
        => OnShow?.Invoke(content, baseOptions, options);

    public void AddNotification(RenderFragment content, NotificationOptions options)
        => OnShow?.Invoke(content, options, null);

    public void AddError(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Error
        }, options);
    }

    public void AddError(string message, Action<NotificationOptions> options = null)
        => AddError(RfNotificationManager.MessageOnly(message), options);

    public void AddError(string message, string title, Action<NotificationOptions> options = null)
        => AddError(RfNotificationManager.MessageAndTitle((message, title)), options);

    public void AddInfo(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Info
        }, options);
    }

    public void AddInfo(string message, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificationManager.MessageOnly(message), options);

    public void AddInfo(string message, string title, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificationManager.MessageAndTitle((message, title)), options);


    public void AddSuccess(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Success
        }, options);
    }

    public void AddSuccess(string message, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificationManager.MessageOnly(message), options);

    public void AddSuccess(string message, string title, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificationManager.MessageAndTitle((message, title)), options);


    public void AddWarning(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Warning
        }, options);
    }

    public void AddWarning(string message, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificationManager.MessageOnly(message), options);

    public void AddWarning(string message, string title, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificationManager.MessageAndTitle((message, title)), options);

}
