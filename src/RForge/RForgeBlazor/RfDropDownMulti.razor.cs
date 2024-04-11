using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RForge.Abstractions;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// A dropdown that supports multiple selections.
/// </summary>
/// <typeparam name="TItem">The data type of a item within the dropdown.</typeparam>
public partial class RfDropDownMulti<TItem> : RfDropDownBase<TItem>
{
    #region Parameters
    [Parameter]
    ///<summary>
    /// The list of selected items.
    /// </summary>
    public List<TItem> SelectedItems { get; set; }
    [Parameter]
    public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

    [Parameter]
    ///<summary>
    /// If set the drop down will not keep more then max selectable items. Refer to <see cref="MaxSelectionKeepRule"/> for how to handle selections over <see cref="MaxSelectableItems" />
    /// </summary>
    public int? MaxSelectableItems { get; set; }

    [Parameter]
    ///<summary>
    /// The rules to follow when a user selects more than <see cref="MaxSelectableItems"/> items. By default uses: <see cref="RfKeepRule.FirstInFirstOut"/>.
    /// </summary>
    public RfKeepRule MaxSelectionKeepRule { get; set; } = RfKeepRule.FirstInFirstOut;
    #endregion

    private bool IsSelected(TItem item)
    {
        if (SelectedItems == null || SelectedItems.Count == 0) return false;

        return SelectedItems.Any(i => ItemComparer(i, item) == true);
    }

    private async Task OnItemClick(TItem item)
    {
        if (IsReadonly == true) return;

        bool isSelected = IsSelected(item);

        if (isSelected == true)
            SelectedItems.Remove(item);

        else if (MaxSelectableItems.HasValue == false
            || MaxSelectableItems <= 0
            || SelectedItems.Count < MaxSelectableItems)
        {
            SelectedItems.Add(item);
        }
        else
        {
            switch (MaxSelectionKeepRule)
            {
                case RfKeepRule.FirstInFirstOut:
                    SelectedItems.RemoveAt(0);
                    break;
                case RfKeepRule.FirstInLastOut:
                    SelectedItems.RemoveAt(SelectedItems.Count - 1);
                    break;
                case RfKeepRule.ForceDeselection:
                    return;
            }
        }

        await SelectedItemsChanged.InvokeAsync(SelectedItems);

        if (CloseOnSelect == true && IsOpen == true)
            await CloseDropDown();

        StateHasChanged();
    }

    private async Task OnItemKeyDown(KeyboardEventArgs e, TItem item)
    {
        if (e.Code == "Enter" || e.Code == "NumpadEnter")
        {
            await OnItemClick(item);
            return;
        }

        if (e.Code == "Escape")
        {
            await CloseDropDown();
        }

    }

    private async Task OnDeselectClick(TItem item)
    {
        if (IsReadonly == true) return;

        if (SelectedItems.Remove(item) == false) return;

        await SelectedItemsChanged.InvokeAsync(SelectedItems);
        StateHasChanged();
    }
}