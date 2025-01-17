using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RForge.Abstractions.DropDowns;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// A dropdown that supports a single selection.
/// </summary>
/// <typeparam name="TItem">The data type of a item within the dropdown.</typeparam>
/// <example>
/// <code>
/// &lt;RfDropDown TItem="string" Items="new List&lt;string&gt; { "Item1", "Item2", "Item3" }" SelectedItem="selectedItem" SelectedItemChanged="OnSelectedItemChanged" /&gt;
/// </code>
/// </example>
public partial class RfDropDown<TItem> : RfDropDownBase<TItem>
{
    #region Parameters
    /// <summary>
    /// The selected item currently selected within the dropdown.
    /// </summary>
    [Parameter]
    public TItem SelectedItem { get; set; }

    /// <summary>
    /// Event callback for when the selected item changes.
    /// </summary>
    [Parameter]
    public EventCallback<TItem> SelectedItemChanged { get; set; }

    #endregion

    /// <summary>
    /// Determines if the item is selected. Makes use of ItemComparer to determine if the item is selected.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item is selected; otherwise, false.</returns>
    private bool IsSelected(TItem item)
    {
        if (SelectedItem == null) return false;

        return ItemComparer(SelectedItem, item);
    }

    /// <summary>
    /// Handles the item click event. Sets the selected item to the clicked item.
    /// </summary>
    /// <param name="item">The item that was clicked.</param>
    private async Task OnItemClick(TItem item)
    {
        if (IsReadonly == true) return;

        bool isSelected = IsSelected(item);

        if (isSelected == true)
        {
            await SelectedItemChanged.InvokeAsync();
        }
        else
        {
            await SelectedItemChanged.InvokeAsync(item);
        }

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

}