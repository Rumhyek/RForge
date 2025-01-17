using Microsoft.AspNetCore.Components;
using RForge.Abstractions.Notifications;

namespace RForgeBlazor.Services;

/// <inheritdoc />
public class NotificationManager : INotificationManager
{
    /// <inheritdoc />
    public event Action OnClearAll;
    /// <inheritdoc />
    public event Action<RfNotificationSeverity> OnClearBySeverity;
    /// <inheritdoc />
    public event Action<RfNotificationPosition> OnClearByPosition;
    /// <inheritdoc />
    public event Action<RenderFragment, NotificationOptions, Action<NotificationOptions>> OnShow;

    /// <inheritdoc />
    public void ClearAll() => OnClearAll?.Invoke();

    /// <inheritdoc />
    public void ClearAllByPostion(RfNotificationPosition position) => OnClearByPosition.Invoke(position);

    /// <inheritdoc />
    public void ClearAllBySeverity(RfNotificationSeverity severity) => OnClearBySeverity.Invoke(severity);

    /// <inheritdoc />
    public void ClearAllError() => ClearAllBySeverity(RfNotificationSeverity.Error);

    /// <inheritdoc />
    public void ClearAllInfo() => ClearAllBySeverity(RfNotificationSeverity.Info);


    /// <inheritdoc />
    public void ClearAllSuccess() => ClearAllBySeverity(RfNotificationSeverity.Success);

    /// <inheritdoc/>
    public void ClearAllWarning() => ClearAllBySeverity(RfNotificationSeverity.Warning);

    /// <inheritdoc/>
    private void CallOnShow(RenderFragment content, NotificationOptions baseOptions, Action<NotificationOptions> options)
        => OnShow?.Invoke(content, baseOptions, options);

    /// <inheritdoc/>
    public void AddNotification(RenderFragment content, NotificationOptions options)
        => OnShow?.Invoke(content, options, null);

    /// <inheritdoc />
    public void AddError(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Error
        }, options);
    }

    /// <inheritdoc/>
    public void AddError(string message, Action<NotificationOptions> options = null)
        => AddError(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public void AddError(string message, string title, Action<NotificationOptions> options = null)
        => AddError(RfNotificationManager.MessageAndTitle((message, title)), options);

    /// <inheritdoc/>
    public void AddInfo(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Info
        }, options);
    }

    /// <inheritdoc/>
    public void AddInfo(string message, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public void AddInfo(string message, string title, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificationManager.MessageAndTitle((message, title)), options);


    /// <inheritdoc/>
    public void AddSuccess(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Success
        }, options);
    }

    /// <inheritdoc/>
    public void AddSuccess(string message, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public void AddSuccess(string message, string title, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificationManager.MessageAndTitle((message, title)), options);


    /// <inheritdoc/>
    public void AddWarning(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Warning
        }, options);
    }

    /// <inheritdoc/>
    public void AddWarning(string message, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public void AddWarning(string message, string title, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificationManager.MessageAndTitle((message, title)), options);

}
