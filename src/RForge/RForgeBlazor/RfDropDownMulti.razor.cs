using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RForge.Abstractions;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// A dropdown that supports multiple selections.
/// </summary>
/// <typeparam name="TItem">The data type of a item within the dropdown.</typeparam>
/// <example>
/// <code>
/// &lt;RfDropDownMulti TItem="string" Items="new List&lt;string&gt; { "Item1", "Item2", "Item3" }" SelectedItems="selectedItems" SelectedItemsChanged="OnSelectedItemsChanged" /&gt;
/// </code>
/// </example>
public partial class RfDropDownMulti<TItem> : RfDropDownBase<TItem>
{
    #region Parameters
    ///<summary>
    /// The list of selected items.
    /// </summary>
    [Parameter]
    public List<TItem> SelectedItems { get; set; }

    /// <summary>
    /// Event callback for when the selected items change.
    /// </summary>
    [Parameter]
    public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

    ///<summary>
    /// If set the drop down will not keep more then max selectable items. Refer to <see cref="MaxSelectionKeepRule"/> for how to handle selections over <see cref="MaxSelectableItems" />
    /// </summary>
    [Parameter]
    public int? MaxSelectableItems { get; set; }

    ///<summary>
    /// The rules to follow when a user selects more than <see cref="MaxSelectableItems"/> items. By default uses: <see cref="RfKeepRule.FirstInFirstOut"/>.
    /// </summary>
    [Parameter]
    public RfKeepRule MaxSelectionKeepRule { get; set; } = RfKeepRule.FirstInFirstOut;
    #endregion

    /// <summary>
    /// Determines if the item is selected.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item is selected; otherwise, false.</returns>
    private bool IsSelected(TItem item)
    {
        if (SelectedItems == null || SelectedItems.Count == 0) return false;

        return SelectedItems.Any(i => ItemComparer(i, item) == true);
    }

    /// <summary>
    /// Handles the item click event. Adds or removes the item from the selected items.
    /// </summary>
    /// <param name="item">The item that was clicked.</param>
    private async Task OnItemClick(TItem item)
    {
        if (IsReadonly == true) return;

        bool isSelected = IsSelected(item);

        if (isSelected == true)
            SelectedItems.RemoveAll(i => ItemComparer(i, item) == true);

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

    /// <summary>
    /// Handles the item key down event. Supports Enter and Escape keys.
    /// </summary>
    /// <param name="e">The keyboard event arguments.</param>
    /// <param name="item">The item that was interacted with.</param>
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

    /// <summary>
    /// Handles the deselect click event. Removes the item from the selected items.
    /// </summary>
    /// <param name="item">The item to deselect.</param>
    private async Task OnDeselectClick(TItem item)
    {
        if (IsReadonly == true) return;

        if (SelectedItems.Remove(item) == false) return;

        await SelectedItemsChanged.InvokeAsync(SelectedItems);
        StateHasChanged();
    }
}