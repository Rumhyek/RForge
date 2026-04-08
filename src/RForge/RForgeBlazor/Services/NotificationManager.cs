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
    public event Action<Guid> OnClearById;
    /// <inheritdoc />
    public event Action<RenderFragment, NotificationOptions, Action<NotificationOptions>> OnShow;
    /// <inheritdoc />
    public event Action<NotificationOptions> OnNotificationDimiss;

    /// <inheritdoc />
    public void ClearAll() => OnClearAll?.Invoke();

    /// <inheritdoc />
    public void ClearAllByPostion(RfNotificationPosition position) => OnClearByPosition?.Invoke(position);

    /// <inheritdoc />
    public void ClearAllBySeverity(RfNotificationSeverity severity) => OnClearBySeverity?.Invoke(severity);
    /// <inheritdoc />
    public void ClearAllError() => ClearAllBySeverity(RfNotificationSeverity.Error);
    /// <inheritdoc />
    public void ClearAllInfo() => ClearAllBySeverity(RfNotificationSeverity.Info);
    /// <inheritdoc />
    public void ClearAllSuccess() => ClearAllBySeverity(RfNotificationSeverity.Success);
    /// <inheritdoc/>
    public void ClearAllWarning() => ClearAllBySeverity(RfNotificationSeverity.Warning);

    /// <inheritdoc/>
    public void ClearById(params Guid[] ids)
    {
        foreach (Guid id in ids)
            OnClearById.Invoke(id);
    }

    /// <inheritdoc/>
    private Guid CallOnShow(RenderFragment content, NotificationOptions baseOptions, Action<NotificationOptions> options)
    {
        OnShow?.Invoke(content, baseOptions, options);

        return baseOptions.NotificationId;
    }

    /// <inheritdoc/>
    public Guid AddNotification(RenderFragment content, NotificationOptions options)
    {
        OnShow?.Invoke(content, options, null);

        return options.NotificationId;
    }

    /// <inheritdoc/>
    public Guid AddNotification(RenderFragment content, Action<NotificationOptions> options)
    {
        return CallOnShow(content, new NotificationOptions(), options);
    }
    /// <inheritdoc />
    public Guid AddError(RenderFragment content, Action<NotificationOptions> options = null)
    {
        return CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Error
        }, options);
    }

    /// <inheritdoc/>
    public Guid AddError(string message, Action<NotificationOptions> options = null)
        => AddError(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public Guid AddError(string message, string title, Action<NotificationOptions> options = null)
        => AddError(RfNotificationManager.MessageAndTitle((message, title)), options);

    /// <inheritdoc/>
    public Guid AddInfo(RenderFragment content, Action<NotificationOptions> options = null)
    {
        return CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Info
        }, options);
    }

    /// <inheritdoc/>
    public Guid AddInfo(string message, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public Guid AddInfo(string message, string title, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificationManager.MessageAndTitle((message, title)), options);


    /// <inheritdoc/>
    public Guid AddSuccess(RenderFragment content, Action<NotificationOptions> options = null)
    {
        return CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Success
        }, options);
    }

    /// <inheritdoc/>
    public Guid AddSuccess(string message, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public Guid AddSuccess(string message, string title, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificationManager.MessageAndTitle((message, title)), options);


    /// <inheritdoc/>
    public Guid AddWarning(RenderFragment content, Action<NotificationOptions> options = null)
    {
        return CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Warning
        }, options);
    }

    /// <inheritdoc/>
    public Guid AddWarning(string message, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificationManager.MessageOnly(message), options);

    /// <inheritdoc/>
    public Guid AddWarning(string message, string title, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificationManager.MessageAndTitle((message, title)), options);

    /// <inheritdoc/>
    public void NotifyNotificationRemoval(NotificationOptions notification)
    {
        OnNotificationDimiss?.Invoke(notification);
    }
}
