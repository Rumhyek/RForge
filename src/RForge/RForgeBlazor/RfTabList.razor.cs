using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RForgeBlazor.Models;

namespace RForgeBlazor;

/// <summary>
/// Represents a tab list component that manages the registration, activation, and navigation of tabs.
/// </summary>
/// <remarks>This component is used to create a tabbed interface where tabs can be dynamically registered,
/// activated, and navigated. It supports keyboard navigation and ensures that only one tab is active at a time. The
/// active tab can be controlled programmatically via the <see cref="ActiveTabId"/> parameter or through user
/// interaction.</remarks>
public partial class RfTabList : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the content to be rendered inside the component Should only be <see cref="RfTab"/> or some sub class.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the currently active tab. This parameter is required.
    /// </summary>
    [Parameter]
    public string ActiveTabId { get; set; }
    /// <summary>
    /// Gets or sets the callback that is invoked when the active tab ID changes.
    /// </summary>
    /// <remarks>This callback is triggered whenever the active tab ID is updated. Use this to handle changes
    /// in the active tab state.</remarks>
    [Parameter]
    public EventCallback<string> ActiveTabIdChanged { get; set; }

    /// <summary>
    /// Gets or sets the ARIA label for the tab list element.
    /// </summary>
    [Parameter]
    public string TabListAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the CSS class or classes to be applied to the component.
    /// </summary>
    /// <remarks>This property allows customization of the component's appearance by specifying CSS classes. 
    /// Ensure that the class names provided are defined in the relevant stylesheets.</remarks>
    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class to be applied to the tabs container.
    /// </summary>
    /// <remarks>Use this property to customize the appearance of the tabs by specifying one or more CSS class
    /// names. Make use of Bulma's .is-boxed, .is-toggle, .is-fullwidth, etc.</remarks>
    [Parameter]
    public string TabsCssClass { get; set; }
    /// <summary>
    /// Gets or sets the CSS class to be applied to the wrapping div around the panel element.
    /// </summary>
    /// <remarks>Use this property to customize the appearance of the panel by specifying one or more CSS
    /// class names. Multiple class names should be separated by spaces.</remarks>
    [Parameter]
    public string PanelCssClass { get; set; }

    /// <summary>
    /// Gets the context associated with the RF tab list.
    /// </summary>
    internal RfTabListContext Context { get; } = new RfTabListContext();

    private List<RfTabInfo> RegisteredTabs { get; } = new List<RfTabInfo>();

    /// <summary>
    /// Invoked when the component is initialized. Subscribes to the <see cref="RfTabListContext.OnTabRegister"/> event.
    /// </summary>
    /// <remarks>This method is called automatically by the framework during the component's initialization
    /// phase. It ensures that the component is registered to handle the <see cref="RfTabListContext.OnTabRegister"/>
    /// event.</remarks>
    protected override void OnInitialized()
    {
        Context.OnTabRegister += Context_OnTabRegister;
    }

    /// <summary>
    /// Invoked when the component's parameters have been set by the parent component.
    /// </summary>
    /// <remarks>This method validates that the <see cref="ActiveTabId"/> parameter is not null, empty, or
    /// consists only of whitespace. If the validation fails, an exception is thrown to indicate that the parameter is
    /// required.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="ActiveTabId"/> parameter is null, empty, or consists only of whitespace.</exception>
    protected override void OnParametersSet()
    {
        if(string.IsNullOrWhiteSpace(ActiveTabId) == true)
            throw new InvalidOperationException("The ActiveTabId parameter is required and cannot be null, empty, or whitespace.");
    }

    private async Task Context_OnTabRegister(object sender, RfTabOnRegisterEventArgs e)
    {
        if (RegisteredTabs.Any(t => t.Tab == e.Tab) == true) return;

        if (RegisteredTabs.Any(t => t.Tab.TabId == e.Tab.TabId) == true)
            throw new InvalidOperationException($"A tab with the ID '{e.Tab.TabId}' is already registered in this tab list. Tab IDs must be unique within a tab list.");

        var newTab = new RfTabInfo(e.Tab);

        RegisteredTabs.Add(newTab);

        if (e.Tab.TabId == ActiveTabId)
        {
            await SetActiveTab(newTab);
        }
    }

    private async Task SetActiveTab(RfTabInfo newTab)
    {
        ActiveTabId = newTab.Tab.TabId;
        await newTab.Tab.TabListOnLoad();
        await newTab.Tab.TabListOnShow();
    }

    private async Task OnTabClick(RfTabInfo tabInfo)
    {
        ActiveTabId = tabInfo.Tab.TabId;
        if (ActiveTabIdChanged.HasDelegate)
            await ActiveTabIdChanged.InvokeAsync(ActiveTabId);

        await tabInfo.Tab.TabListOnLoad();
        await tabInfo.Tab.TabListOnShow();
    }

    private async Task OnTabKeyDown(KeyboardEventArgs e, RfTabInfo tabInfo)
    {
        switch (e.Code)
        {
            case "ArrowRight":
                {
                    var currentIndex = RegisteredTabs.FindIndex(t => t == tabInfo);
                    if (currentIndex != -1)
                    {
                        var nextIndex = currentIndex + 1;
                        if (nextIndex < RegisteredTabs.Count)
                            await RegisteredTabs[nextIndex].TabElement.FocusAsync();
                    }

                    break;
                }
            case "ArrowLeft":
                {
                    var currentIndex = RegisteredTabs.FindIndex(t => t == tabInfo);
                    if (currentIndex != -1)
                    {
                        var previousIndex = currentIndex - 1;
                        if (previousIndex >= 0)
                            await RegisteredTabs[previousIndex].TabElement.FocusAsync();
                    }
                    break;
                }
            case "Home":
                if (RegisteredTabs.Count > 0)
                    await RegisteredTabs[0].TabElement.FocusAsync();
                break;
            case "End":
                if (RegisteredTabs.Count > 0)
                    await RegisteredTabs[RegisteredTabs.Count - 1].TabElement.FocusAsync();
                break;
        }
    }

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    /// <remarks>This method unsubscribes from the <see cref="RfTabListContext.OnTabRegister"/> event if the <see
    /// cref="Context"/> is not null. Call this method when the instance is no longer needed to ensure proper cleanup of
    /// resources.</remarks>
    public void Dispose()
    {
        if(Context != null)
            Context.OnTabRegister -= Context_OnTabRegister;
    }

    private class RfTabInfo
    {
        public RfTabInfo(RfTab tab)
        {
            Tab = tab;
        }

        public RfTab Tab { get; init; }
        public ElementReference TabElement { get; set; }
    }
}
