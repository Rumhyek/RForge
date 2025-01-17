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
            await OnSortChanged.Invoke(this, sort);
    }

    /// <summary>
    /// Raises the <see cref="OnFilterChanged"/> event.
    /// </summary>
    /// <param name="filter">The filter details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FilterChanged(DataGridFilterBy filter)
    {
        if (OnFilterChanged != null)
            await OnFilterChanged.Invoke(this, filter);
    }
}
