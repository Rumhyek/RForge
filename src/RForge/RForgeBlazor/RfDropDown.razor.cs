using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RForge.Abstractions.DropDowns;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// A dropdown that supports a single selection.
/// </summary>
/// <typeparam name="TItem">The data type of a item within the dropdown.</typeparam>
public partial class RfDropDown<TItem> : RfDropDownBase<TItem>
{
    #region Parameters
    /// <summary>
    /// The selected item currently selected within the dropdown.
    /// </summary>
    [Parameter]
    public TItem SelectedItem { get; set; }
    [Parameter]
    public EventCallback<TItem> SelectedItemChanged { get; set; }

    /// <summary>
    /// Determines how to render the selected item within a drop down options. Does not affect the <see cref="RfDropDownBase{TItem}.Items"/> output.
    /// </summary>
    [Parameter]
    public RfShowSelectionInDropDown ShowSelectedItemInDropDown { get; set; } = RfShowSelectionInDropDown.OnlyWhenNotInList;
    #endregion


    private bool IsSelected(TItem item)
    {
        if (SelectedItem == null) return false;

        return ItemComparer(SelectedItem, item);
    }

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