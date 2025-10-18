using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForgeBlazor
{
    /// <summary>
    /// Represents a tab component that can display content within a tabbed interface.
    /// </summary>
    /// <remarks>The <see cref="RfTab"/> component allows developers to define the content of a tab and its
    /// associated panel, customize their appearance using CSS classes, and handle events such as when the tab is shown
    /// or loaded. This component is typically used in conjunction with a tab container to create a tabbed user
    /// interface.</remarks>
    public partial class RfTab : ComponentBase
    {
        /// <summary>
        /// Gets or sets the unique identifier for the tab.
        /// </summary>
        [Parameter]
        public string TabId { get; set; }

        /// <summary>
        /// Gets or sets the content to be rendered inside the tab.
        /// </summary>
        /// <remarks>Use this property to define the UI elements or components that should appear within
        /// the tab.</remarks>
        [Parameter]
        public RenderFragment Tab { get; set; }

        /// <summary>
        /// Gets or sets the content to be rendered inside the panel.
        /// </summary>
        [Parameter]
        public RenderFragment Panel { get; set; }

        /// <summary>
        /// Gets or sets the content to be displayed while the tab is loading.
        /// </summary>
        [Parameter]
        public RenderFragment LoadingPanel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the panel should be rendered hidden even when it is not active.
        /// </summary>
        /// <remarks>Use this to improve SEO by rendering the content even while not visible. 
        /// Note: This will still only call <see cref="OnLoad"/> when showing the tab.</remarks>
        [Parameter]
        public bool ShowPanelWhenHidden { get; set; } = false;
        /// <summary>
        /// Gets or sets the CSS class to be applied to the panel element.
        /// </summary>
        /// <remarks>Use this property to customize the appearance of the panel by specifying one or more
        /// CSS class names. Multiple class names can be provided, separated by spaces.</remarks>
        [Parameter]
        public string PanelCssClass { get; set; }
        /// <summary>
        /// Gets or sets the CSS class applied to the tab element.
        /// </summary>
        /// <remarks>Use this property to customize the appearance of the tab element by specifying one or
        /// more CSS class names.</remarks>
        [Parameter]
        public string TabCssClass { get; set; }
        /// <summary>
        /// Gets or sets the callback that is invoked when the component is shown.
        /// </summary>
        /// <remarks>Use this callback to handle actions or logic that should occur when the component
        /// becomes visible.  The parameter passed to the callback is the tab id set for this tab.</remarks>
        [Parameter]
        public EventCallback<string> OnShow { get; set; }

        /// <summary>
        /// Gets or sets the callback that is invoked when a load event occurs.
        /// </summary>
        /// <remarks>The callback can be used to handle load events and perform custom logic based on the
        /// provided tab id.</remarks>
        [Parameter]
        public EventCallback<string> OnLoad { get; set; }

        /// <summary>
        /// Invoked when the component is hidden.
        /// </summary>
        [Parameter]
        public EventCallback<string> OnHide { get; set; }

        /// <summary>
        /// Gets or sets the cascading parameter that provides the context for the associated tab list.
        /// </summary>
        /// <remarks>This property is marked with the <see cref="CascadingParameterAttribute"/>,
        /// indicating that the value is supplied by an ancestor component in the component hierarchy.</remarks>
        [CascadingParameter]
        public RfTabListContext Context { get; set; }

        internal bool IsLoaded { get; set; } = false;
        internal bool IsLoading { get; set; } = false;
        internal bool IsShown { get; set; } = false;

        /// <summary>
        /// Asynchronously handles parameter changes and ensures the component is properly registered within its
        /// context.
        /// </summary>
        /// <remarks>This method validates that the required <see cref="TabId"/> parameter is provided and
        /// that the component is used within a valid <see cref="RfTabList"/> context. If these conditions are met, it
        /// registers the tab with the parent context by invoking the <see cref="RfTabListContext.RaiseOnTabRegister"/>
        /// method.</remarks>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the TabId parameter is null, empty, or consists only of whitespace. Thrown if the component is not
        /// used within an RfTabList context.</exception>
        protected override async Task OnParametersSetAsync()
        {
            if(string.IsNullOrWhiteSpace(TabId) == true)
                throw new InvalidOperationException("The TabId parameter is required and cannot be null, empty, or whitespace.");
            
            if(Context == null)
                throw new InvalidOperationException("The RfTab component must be used within an RfTabList component.");
            
            await Context.RaiseOnTabRegister(new RfTabOnRegisterEventArgs(this));
        }

        internal async Task TabListOnShow()
        {
            IsShown = true;

            if (OnShow.HasDelegate == true)
                await OnShow.InvokeAsync(TabId);
        }

        internal async Task TabListOnLoad()
        {
            if (IsLoading == true || IsLoaded == true) return;

            IsLoading = true;

            if (OnLoad.HasDelegate == true)
                await OnLoad.InvokeAsync(TabId);

            IsLoading = false;
            IsLoaded = true;
        }

        internal async Task TabListOnHide()
        {
            IsShown = false;
            if (OnHide.HasDelegate == true)
                await OnHide.InvokeAsync(TabId);
        }
    }
}
