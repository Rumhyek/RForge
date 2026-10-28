using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;
using RForgeBlazor.Services;
using System.Collections.Concurrent;
using System.Threading;

namespace RForgeBlazor.Models;

/// <summary>
/// Base class for a dropdown component in Blazor.
/// </summary>
/// <typeparam name="TItem">The type of the items in the dropdown.</typeparam>
public class RfDropDownBase<TItem> : ComponentBase, IDisposable
{
    #region Parameters
    /// <summary>
    /// How to render a <typeparamref name="TItem"/> within the dropdown and select input.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem> ChildContent { get; set; }

    /// <summary>
    /// The template to render a item in the dropdown. By default uses <see cref="ChildContent"/>
    /// </summary>
    [Parameter]
    public RenderFragment<TItem> RowItemTemplate { get; set; }

    /// <summary>
    /// The template to render a selected item. By default uses <see cref="ChildContent"/>
    /// </summary>
    [Parameter]
    public RenderFragment<TItem> SelectedItemTemplate { get; set; }

    /// <summary>
    /// Determines how to render the selected item within a drop down options. Does not affect the <see cref="RfDropDownBase{TItem}.Items"/> output.
    /// </summary>
    [Parameter]
    public RfShowSelectionInDropDown ShowSelectedItemInDropDown { get; set; } = RfShowSelectionInDropDown.OnlyWhenNotInList;

    /// <summary>
    /// Used to compare two items within the list to determine if it is selected or not. By default uses <typeparamref name="TItem"/> Equals(object?)/>
    /// </summary>
    [Parameter]
    public Func<TItem, TItem, bool> ItemComparer { get; set; }

    /// <summary>
    /// The css class added to the dropdown itself.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// The css class to add to the selected items.
    /// </summary>
    [Parameter]
    public string CssSelectedClass { get; set; }

    /// <summary>
    /// The css class to add to the row items in the drop down.
    /// </summary>
    [Parameter]
    public string CssRowItemClass { get; set; }

    /// <summary>
    /// The css class to add to the filter input.
    /// </summary>
    [Parameter]
    public string CssFilterClass { get; set; }

    /// <summary>
    /// If true the filter textbox will be shown. By default = true
    /// </summary>
    [Parameter]
    public bool ShowFilter { get; set; } = true;

    /// <summary>
    /// The current filter text. Is a two way binding.
    /// </summary>
    [Parameter]
    public string Filter { get; set; }
    /// <summary>
    /// The current filter text. Is a two way binding.
    /// </summary>
    [Parameter]
    public EventCallback<string> FilterChanged { get; set; }

    /// <summary>
    /// The filter icon to show in the input.
    /// </summary>
    [Parameter]
    public string FilterIcon { get; set; } = "fa-solid fa-magnifying-glass";

    /// <summary>
    /// The placeholder text to show in the fitler input.
    /// </summary>
    [Parameter]
    public string FilterPlaceholderText { get; set; } = "Filter...";

    /// <summary>
    /// The rate in milliseconds that the search filter will wait before attempting to call OnLoad.
    /// </summary>
    [Parameter]
    public int FilterRateLimit { get; set; } = 250;

    /// <summary>
    /// If true the filter text will reset to null on close.
    /// </summary>
    [Parameter]
    public bool FilterClearOnClose { get; set; } = true;

    /// <summary>
    /// The items to show in the dropdown.
    /// </summary>
    [Parameter]
    public List<TItem> Items { get; set; }

    /// <summary>
    /// When <code>Items == null || Items.Count == 0</code> This text is shown in the dropdown
    /// unless <see cref="EmptyContentTemplate"/> is set.
    /// </summary>
    [Parameter]
    public string EmptyContentText { get; set; } = "No Items Found";

    /// <summary>
    /// When <code>Items == null || Items.Count == 0</code>. If set this is called instead of using EmptyContentText and
    /// instead passes in EmptyContentText as the context.
    /// </summary>
    [Parameter]
    public RenderFragment<string> EmptyContentTemplate { get; set; }

    /// <summary>
    /// The placeholder text for when no selection has been made.
    /// </summary>
    [Parameter]
    public string PlaceholderText { get; set; } = "No Items";

    /// <summary>
    /// If set to true the dropdown will open. Is two way binding.
    /// </summary>
    [Parameter]
    public bool IsOpen { get; set; }
    /// <summary>
    /// Event callback for when the IsOpen parameter changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// If true on selection the dropdown will close.
    /// </summary>
    [Parameter]
    public bool CloseOnSelect { get; set; }

    /// <summary>
    /// Called when the drop down is opening. Passing the current filter value. This is called after the OnLoad.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnOpen { get; set; }

    /// <summary>
    /// Called when the drop down is closed. Passing the current filter value.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnClose { get; set; }

    /// <summary>
    /// Called when the drop down believes data needs to be loaded. Should set <see cref="Items"/>.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnLoad { get; set; }

    /// <summary>
    /// The direction to open the drop down.
    /// </summary>
    [Parameter]
    public RfDropDownPosition Position { get; set; } = RfDropDownPosition.LeftDown;

    /// <summary>
    /// If true the items within the dropdown cannot be modified within.
    /// </summary>
    [Parameter]
    public bool IsReadonly { get; set; }
    #endregion

    /// <summary>
    /// Unique identifier for this dropdown component instance.
    /// </summary>
    protected string DropdownId { get; private set; } = $"dropdown-menu-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets or sets a value indicating whether the dropdown is loading.
    /// </summary>
    protected bool IsLoading { get; set; }

    /// <summary>
    /// Handles the click event for the dropdown. Is ignored if <see cref="IsReadonly"/> is true.
    /// </summary>
    protected virtual async Task OnDropDownClick()
    {
        if (IsReadonly == true) return;

        if (IsOpen == true)
            await CloseDropDown();
        else
            await OpenDropDown();
    }

    private readonly Func<TItem, TItem, bool> _defaultItemComparer = (i1, i2) => EqualityComparer<TItem>.Default.Equals(i1, i2);

    /// <summary>
    /// Sets the parameters for the component.
    /// </summary>
    protected override void OnParametersSet()
    {
        ItemComparer ??= _defaultItemComparer;
    }

    private ConcurrentStack<FilterEventArgs> _filterChanges = new ConcurrentStack<FilterEventArgs>();
    private Task _filterHandler = null;
    // #71: the filter pump gets one CancellationTokenSource per generation. Close, reopen and
    // Dispose cancel the in-flight pump instead of orphaning it; a fire-and-forget Task's own
    // Dispose() cancels nothing, which is what let CANARY-2 call back post-dispose.
    private CancellationTokenSource _filterCts = null;

    private async Task handleFilters(CancellationToken ct)
    {
        try
        {
            while (_filterChanges.IsEmpty == false && ct.IsCancellationRequested == false)
            {
                if (FilterRateLimit > 0)
                {
                    DateTime lastChecked = DateTime.Now;
                    bool rateLimited = false;

                    do
                    {
                        if (_filterChanges.TryPeek(out var filter) == true)
                        {
                            if (DateTime.Now.Subtract(filter.DateRecorded).TotalMilliseconds >= FilterRateLimit)
                            {
                                rateLimited = true;
                            }
                            else
                            {
                                await Task.Delay((int)DateTime.Now.Subtract(filter.DateRecorded).TotalMilliseconds + 1, ct);
                            }
                        }
                        else
                        {
                            //something is locking the peek give it some time
                            await Task.Delay(10, ct);
                            continue;
                        }
                    } while (rateLimited == false);
                }

                // #71: cancelled while rate-waiting (close/reopen/dispose): do not start a load.
                if (ct.IsCancellationRequested == true) return;

                if (_filterChanges.TryPop(out var changes) == true)
                {
                    _filterChanges.Clear();

                    if (IsOpen == false) return;

                    IsLoading = true;
                    StateHasChanged();

                    try
                    {
                        await FilterChanged.InvokeAsync(changes.Filter);

                        // #71 (CANARY-2): cancelled/disposed while awaiting a consumer callback -
                        // the remaining callback must not fire into a closed or disposed component.
                        ct.ThrowIfCancellationRequested();

                        await OnLoad.InvokeAsync((string)changes.Filter);
                    }
                    finally
                    {
                        // #71 (same class as #70): a consumer load that throws must not latch
                        // IsLoading forever — the pump is fire-and-forget, nobody would ever
                        // clear it. Success path is unchanged: clear, then re-render.
                        IsLoading = false;

                        // #71: never touch the renderer after cancel/dispose.
                        if (ct.IsCancellationRequested == false)
                            StateHasChanged();
                    }

                    // #71: cancelled/disposed while the load was awaited: stop; a later pump
                    // generation (or the already running one) owns the filter stack.
                    if (ct.IsCancellationRequested == true) return;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested == true)
        {
            // #71: close/reopen/dispose cancelled this pump generation - expected unwind.
        }
        catch (Exception)
        {
            // #71: a consumer load threw. The IsLoading latch was already cleared in the
            // finally above. The pump must not rethrow: it is started fire-and-forget from
            // keystroke dispatch, so a rethrow either crashes the dispatch (synchronous throw)
            // or dies as an unobserved task fault. Stop this generation quietly; the next
            // keystroke starts a fresh pump.
        }
    }

    /// <summary>
    /// Handles the change event for the filter input.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    protected void OnFilterChange(ChangeEventArgs args)
    {

        _filterChanges.Push(new FilterEventArgs((string)args.Value));

        if (_filterHandler == null || _filterHandler.IsCompleted == true)
        {
            // #71: each pump generation carries its own CTS so Close/Open/Dispose can cancel
            // an in-flight pump instead of orphaning it (the double-pump race).
            _filterCts?.Dispose();
            _filterCts = new CancellationTokenSource();
            _filterHandler = handleFilters(_filterCts.Token);
        }
    }

    /// <summary>
    /// Cancels any in-flight filter pump and clears pending filter changes. Used by close,
    /// reopen and dispose so at most one pump is ever alive (#71).
    /// </summary>
    private void CancelFilterPump()
    {
        _filterChanges.Clear();

        if (_filterCts != null)
        {
            _filterCts.Cancel();
            _filterCts.Dispose();
            _filterCts = null;
        }

        _filterHandler = null;
    }

    /// <summary>
    /// Closes the dropdown. If <see cref="IsOpen"/> is false it will not close.
    /// </summary>
    protected async Task CloseDropDown()
    {
        if (IsOpen == false) return;

        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);

        // #71: cancel the pump instead of nulling the field without cancelling it.
        CancelFilterPump();
        if (FilterClearOnClose == true)
        {
            Filter = null;
            await FilterChanged.InvokeAsync(Filter);
        }

        await OnClose.InvokeAsync(Filter);

    }

    /// <summary>
    /// Opens the dropdown. If <see cref="IsLoading"/> is true it will not open.
    /// </summary>
    protected async Task OpenDropDown()
    {
        if (IsLoading == true) return;

        IsLoading = true;
        StateHasChanged();
        try
        {
            await OnLoad.InvokeAsync(Filter);
        }
        finally
        {
            // #70: a consumer OnLoad that throws must not latch IsLoading forever —
            // the entry guard above would make this dropdown permanently unopenable.
            // Success path is unchanged (same order: clear, re-render, then open).
            IsLoading = false;
            StateHasChanged();
        }

        // #71: cancel any surviving pump (e.g. orphaned by a close) before starting fresh.
        CancelFilterPump();
        IsOpen = true;
        await IsOpenChanged.InvokeAsync(true);
        await OnOpen.InvokeAsync(Filter);

    }

    /// <summary>
    /// Disposes the component.
    /// </summary>
    public void Dispose()
    {
        // #71 (CANARY-2): Task.Dispose() on the pump cancels nothing - cancel it via its CTS
        // so an in-flight filter load cannot invoke consumer callbacks or touch this
        // component after dispose.
        CancelFilterPump();

        GC.SuppressFinalize(this);
    }

    #region Computeds

    /// <summary>
    /// Gets the CSS classes for the dropdown.
    /// </summary>
    protected string DropdownCss
    {
        get
        {
            string css = null;

            CssHelper.AddIfTrue(ref css, IsOpen && IsReadonly == false, "is-active");

            CssHelper.AddIfTrue(ref css, Position == RfDropDownPosition.RightDown
                || Position == RfDropDownPosition.RightUp, "is-right");

            CssHelper.AddIfTrue(ref css, Position == RfDropDownPosition.LeftUp
                || Position == RfDropDownPosition.RightUp, "is-up");

            CssHelper.AddIfTrue(ref css, string.IsNullOrWhiteSpace(CssClass) == false, CssClass);

            return css;
        }
    }

    /// <summary>
    /// Gets the CSS classes for the filter input, combining the Bulma <c>input</c> class
    /// with the user-supplied <see cref="CssFilterClass"/> when one is set.
    /// </summary>
    protected string FilterInputCss
    {
        get
        {
            string css = "input";

            CssHelper.AddIfTrue(ref css, string.IsNullOrWhiteSpace(CssFilterClass) == false, CssFilterClass);

            return css;
        }
    }

    /// <summary>
    /// Gets the CSS classes for the loading state.
    /// </summary>
    protected string IsLoadingCss
    {
        get
        {
            string css = null;

            if (IsLoading == true)
                css = "is-loading";

            return css;
        }
    }

    #endregion

    /// <summary>
    /// Represents the filter event arguments.
    /// </summary>
    internal class FilterEventArgs
    {
        /// <summary>
        /// Gets or sets the date and time when the filter event was recorded.
        /// </summary>
        internal DateTime DateRecorded { get; set; } = DateTime.Now;
        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
        internal string Filter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterEventArgs"/> class.
        /// </summary>
        /// <param name="filter">The filter text.</param>
        internal FilterEventArgs(string filter)
        {
            Filter = filter;
        }
    }
}