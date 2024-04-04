using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DataGrids;
using RForge.Abstractions.Notifications;

namespace RForgeBlazor.Models;

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

public class DataGridContext
{
    /// <summary>
    /// An event that is raised when a the sort changes
    /// </summary>
    public event AsyncEventHandler<DataGridSortBy> OnSortChanged;
    /// <summary>
    /// An event that is raised when a fitler changes.
    /// </summary>
    public event AsyncEventHandler<DataGridFilterBy> OnFilterChanged;


    public async Task SortChanged(DataGridSortBy sort)
    {
        if (OnSortChanged != null)
            await OnSortChanged.Invoke(this, sort);
    }

    public async Task FilterChanged(DataGridFilterBy filter)
    {
        if (OnFilterChanged != null)
            await OnFilterChanged.Invoke(this, filter);
    }
}

public interface INotificationManager : INotificationManagerBackend
{

    /// <summary>
    /// A event that will be invoked to clear all toasts
    /// </summary>
    event Action OnClearAll;

    /// <summary>
    /// A event that will be invoked to clear toast of specified level
    /// </summary>
    event Action<RfNotificationSeverity> OnClearBySeverity;

    /// <summary>
    /// A event that will be invoked to clear toast of specified position.
    /// </summary>
    event Action<RfNotificationPosition> OnClearByPosition;

    /// <summary>
    /// A event that will be invoked when showing a notification.
    /// </summary>
    event Action<RenderFragment, NotificationOptions, Action<NotificationOptions>> OnShow;

    /// <summary>
    /// Shows a information notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddInfo(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a success notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddSuccess(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a warning notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddWarning(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a error notification 
    /// </summary>
    /// <param name="content">RenderFragment to display on the notification</param>
    /// <param name="options">Options to configure the notification instance</param>
    void AddError(RenderFragment content, Action<NotificationOptions> options = null);

    /// <summary>
    /// Shows a notification using the supplied options
    /// </summary>
    /// <param name="content">RenderFragment to display inside the notification</param>
    /// <param name="options">Options to configure the notification instance. If null the system will use the default options.</param>
    void AddNotification(RenderFragment content, NotificationOptions options);


    /// <summary>
    /// Removes all notifications
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Removes all notifications with a specified <paramref name="RfNotificationSeverity"/>.
    /// </summary>
    void ClearAllBySeverity(RfNotificationSeverity severity);

    /// <summary>
    /// Removes all notifications with a specified <paramref name="RfNotificationPosition"/>.
    /// </summary>
    void ClearAllByPostion(RfNotificationPosition position);

    /// <summary>
    /// Removes all notifications with notification level warning
    /// </summary>
    void ClearAllWarning();

    /// <summary>
    /// Removes all notifications with notification level Info
    /// </summary>
    void ClearAllInfo();

    /// <summary>
    /// Removes all notifications with notification level Success
    /// </summary>
    void ClearAllSuccess();

    /// <summary>
    /// Removes all notifications with notification level Error
    /// </summary>
    void ClearAllError();
}

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
        => AddError(RfNotificatonManager.MessageOnly(message), options);

    public void AddError(string message, string title, Action<NotificationOptions> options = null)
        => AddError(RfNotificatonManager.MessageAndTitle((message, title)), options);

    public void AddInfo(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Info
        }, options);
    }

    public void AddInfo(string message, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificatonManager.MessageOnly(message), options);

    public void AddInfo(string message, string title, Action<NotificationOptions> options = null)
        => AddInfo(RfNotificatonManager.MessageAndTitle((message, title)), options);


    public void AddSuccess(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Success
        }, options);
    }

    public void AddSuccess(string message, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificatonManager.MessageOnly(message), options);

    public void AddSuccess(string message, string title, Action<NotificationOptions> options = null)
        => AddSuccess(RfNotificatonManager.MessageAndTitle((message, title)), options);


    public void AddWarning(RenderFragment content, Action<NotificationOptions> options = null)
    {
        CallOnShow(content, new NotificationOptions()
        {
            Severity = RfNotificationSeverity.Warning
        }, options);
    }

    public void AddWarning(string message, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificatonManager.MessageOnly(message), options);

    public void AddWarning(string message, string title, Action<NotificationOptions> options = null)
        => AddWarning(RfNotificatonManager.MessageAndTitle((message, title)), options);

}
