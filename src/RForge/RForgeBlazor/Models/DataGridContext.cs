using RForge.Abstractions;
using RForge.Abstractions.DataGrids;

namespace RForgeBlazor.Models;

/// <summary>
/// Represents an asynchronous event handler.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event arguments.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

/// <summary>
/// Represents an asynchronous event handler that returns a boolean value.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event arguments.</param>
/// <returns>A task representing the asynchronous operation that returns a boolean value.</returns>
public delegate Task<bool> AsyncBoolEventHandler<TEventArgs>(object sender, TEventArgs e);

/// <summary>
/// Represents an asynchronous event handler that returns a string value.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event arguments.</param>
/// <returns>A task representing the asynchronous operation that returns a string value.</returns>
public delegate Task<string> AsyncStringEventHandler<TEventArgs>(object sender, TEventArgs e);

/// <summary>
/// Represents the context for a data grid, including events for sorting and filtering.
/// </summary>
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

    /// <summary>
    /// Gets or sets the initial sort key.
    /// </summary>
    public string InitialSortKey { get; set; }
    /// <summary>
    /// Gets or sets the initial sort order.
    /// </summary>
    public RfSortOrder InitialSortOrder { get; set; }

    /// <summary>
    /// Raises the <see cref="OnSortChanged"/> event.
    /// </summary>
    /// <param name="sort">The sort details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SortChanged(DataGridSortBy sort)
    {
        if (OnSortChanged != null)
            await AsyncEventInvoker.InvokeAllAsync(OnSortChanged, this, sort);
    }

    /// <summary>
    /// Raises the <see cref="OnFilterChanged"/> event.
    /// </summary>
    /// <param name="filter">The filter details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FilterChanged(DataGridFilterBy filter)
    {
        if (OnFilterChanged != null)
            await AsyncEventInvoker.InvokeAllAsync(OnFilterChanged, this, filter);
    }
}

/// <summary>
/// Raises multicast <see cref="AsyncEventHandler{TEventArgs}"/> events so that every subscriber
/// is awaited. Shared by the context raisers so the fault policy is identical everywhere.
/// </summary>
/// <remarks>
/// Invoking a task-returning multicast delegate with <c>Delegate.Invoke</c> runs every subscriber
/// but returns only the LAST subscriber's task, so earlier subscribers run fire-and-forget: their
/// exceptions go unobserved and the raiser returns while their work is still in flight. This helper
/// instead invokes each entry of <see cref="Delegate.GetInvocationList"/> individually and awaits
/// every returned task via <see cref="Task.WhenAll(IEnumerable{Task})"/>.
/// Fault policy (identical for every context that uses this helper): all subscribers always run to
/// completion; if exactly one subscriber faults, its exception is rethrown unchanged; if more than
/// one faults, the faults are combined into a single <see cref="AggregateException"/> containing
/// every subscriber exception.
/// </remarks>
internal static class AsyncEventInvoker
{
    /// <summary>
    /// Invokes every subscriber of a multicast async event handler and awaits all of their tasks.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
    /// <param name="multicast">The multicast event delegate to invoke. Must not be <c>null</c>.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    /// <returns>A task that completes when every subscriber's task has completed.</returns>
    internal static async Task InvokeAllAsync<TEventArgs>(AsyncEventHandler<TEventArgs> multicast, object sender, TEventArgs e)
    {
        var subscribers = multicast.GetInvocationList();
        var tasks = new List<Task>(subscribers.Length);
        foreach (AsyncEventHandler<TEventArgs> subscriber in subscribers)
        {
            try
            {
                tasks.Add(subscriber.Invoke(sender, e));
            }
            catch (Exception ex)
            {
                // A subscriber that throws before its first await must not stop the others.
                tasks.Add(Task.FromException(ex));
            }
        }

        var all = Task.WhenAll(tasks);
        try
        {
            await all;
        }
        catch
        {
            if (all.Exception?.InnerExceptions.Count > 1)
                throw new AggregateException("One or more async event subscribers failed.", all.Exception.InnerExceptions);
            throw;
        }
    }
}
