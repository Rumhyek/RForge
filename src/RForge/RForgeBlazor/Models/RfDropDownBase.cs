using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;
using RForgeBlazor.Services;
using System.Collections.Concurrent;

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

    private readonly Func<TItem, TItem, bool> _defaultItemComparer = private static readonly Func<TItem, TItem, bool> _defaultItemComparer = (i1, i2) => EqualityComparer<TItem>.Default.Equals(i1, i2);
    /// <summary>
    /// Sets the parameters for the component.
    /// </summary>
    protected override void OnParametersSet()
    {
        ItemComparer ??= _defaultItemComparer;
    }

    private ConcurrentStack<FilterEventArgs> _filterChanges = new ConcurrentStack<FilterEventArgs>();
    private Task _filterHandler = null;

    private async Task handleFilters()
    {
        while (_filterChanges.IsEmpty == false)
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
                            await Task.Delay((int)DateTime.Now.Subtract(filter.DateRecorded).TotalMilliseconds + 1);
                        }
                    }
                    else
                    {
                        //something is locking the peek give it some time
                        await Task.Delay(10);
                        continue;
                    }
                } while (rateLimited == false);
            }

            if (_filterChanges.TryPop(out var changes) == true)
            {
                _filterChanges.Clear();

                if (IsOpen == false) return;

                IsLoading = true;
                StateHasChanged();
                await FilterChanged.InvokeAsync(changes.Filter);
                await OnLoad.InvokeAsync((string)changes.Filter);
                IsLoading = false;
                StateHasChanged();
            }
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
            _filterHandler = handleFilters();
    }

    /// <summary>
    /// Closes the dropdown. If <see cref="IsOpen"/> is false it will not close.
    /// </summary>
    protected async Task CloseDropDown()
    {
        if (IsOpen == false) return;

        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);

        _filterChanges.Clear();
        _filterHandler = null;
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
        await OnLoad.InvokeAsync(Filter);
        IsLoading = false;
        StateHasChanged();

        _filterChanges.Clear();
        _filterHandler = null;
        IsOpen = true;
        await IsOpenChanged.InvokeAsync(true);
        await OnOpen.InvokeAsync(Filter);

    }

    /// <summary>
    /// Disposes the component.
    /// </summary>
    public void Dispose()
    {
        _filterChanges.Clear();
        _filterHandler?.Dispose();

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