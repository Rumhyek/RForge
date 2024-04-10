using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;
using RForgeBlazor.Services;
using System.Collections.Concurrent;

namespace RForgeBlazor.Models;

public class RfDropDownBase<TItem> : ComponentBase, IDisposable
{

    [Parameter]
    public RenderFragment<TItem> ChildContent { get; set; }

    [Parameter]
    public RenderFragment<TItem> RowItemTemplate { get; set; }

    [Parameter]
    public RenderFragment<TItem> SelectedItemTemplate { get; set; }



    [Parameter]
    public Func<TItem, TItem, bool> ItemComparer { get; set; }

    [Parameter]
    public string CssClass { get; set; }

    [Parameter]
    public bool ShowFilter { get; set; } = true;

    [Parameter]
    public string Filter { get; set; }
    [Parameter]
    public EventCallback<string> FilterChanged { get; set; }

    [Parameter]
    public string FilterIcon { get; set; } = "fa-solid fa-magnifying-glass";

    [Parameter]
    public string FilterPlaceholderText { get; set; } = "Filter...";
    
    [Parameter]
    public int FilterRateLimit { get; set; } = 250;

    [Parameter]
    public bool FilterClearOnClose { get; set; } = true;

    [Parameter]
    public List<TItem> Items { get; set; }

    [Parameter]
    public string EmptyContentText { get; set; } = "No Items Found";

    [Parameter]
    public string PlaceholderText { get; set; } = "No Items";

    [Parameter]
    public bool IsOpen { get; set; }
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter]
    public bool CloseOnSelect { get; set; }

    [Parameter]
    public EventCallback<string> OnOpen { get; set; }
    [Parameter]
    public EventCallback<string> OnClose { get; set; }
    [Parameter]
    public EventCallback<string> OnLoad { get; set; }

    [Parameter]
    public RfDropDownPosition Position { get; set; } = RfDropDownPosition.LeftDown;

    [Parameter]
    public bool IsReadonly { get; set; }

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
        if(IsOpen == false) return;

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