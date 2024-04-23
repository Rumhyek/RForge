using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;
using RForgeBlazor.Services;
using System.Collections.Concurrent;

namespace RForgeBlazor.Models;

public class RfDropDownBase<TItem> : ComponentBase, IDisposable
{
    #region Parameters
    /// <summary>
    /// How to render a <see cref="TItem"/> within the dropdown and select input.
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
    /// Used to compare two items within the list to determine if it is selected or not. By default uses <see cref="TItem.Equals(object?)"/>
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

    protected bool IsLoading { get; set; }

    protected virtual async Task OnDropDownClick()
    {
        if (IsReadonly == true) return;

        if (IsOpen == true)
            await CloseDropDown();
        else
            await OpenDropDown();
    }

    private readonly Func<TItem, TItem, bool> _defaultItemComparer = (i1, i2) => i1.Equals(i2);
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

    protected void OnFilterChange(ChangeEventArgs args)
    {

        _filterChanges.Push(new FilterEventArgs((string)args.Value));

        if (_filterHandler == null || _filterHandler.IsCompleted == true)
            _filterHandler = handleFilters();
    }

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

    public void Dispose()
    {
        _filterChanges.Clear();
        _filterHandler?.Dispose();

        GC.SuppressFinalize(this);
    }

    #region Computeds

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

    internal class FilterEventArgs
    {
        internal DateTime DateRecorded { get; set; } = DateTime.Now;
        internal string Filter { get; set; }

        internal FilterEventArgs(string filter)
        {
            Filter = filter;
        }
    }
}