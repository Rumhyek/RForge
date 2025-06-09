namespace RForge.Abstractions.Modal;

/// <summary>
/// Represents configuration options for a multi-action button in a dialog.
/// </summary>
/// <remarks>This class provides properties to define the appearance, behavior, and associated data for a button
/// used in dialog components. It includes options for text, action handling, CSS styling, and state management such as
/// loading or disabled states.</remarks>
public class RfDialogMultiActionButtonOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RfDialogMultiActionButtonOptions"/> class.
    /// </summary>
    /// <remarks>This constructor creates a default instance of the <see
    /// cref="RfDialogMultiActionButtonOptions"/> class. Use this class to configure options for multi-action buttons in
    /// RF dialogs.</remarks>
    public RfDialogMultiActionButtonOptions()
    {
        
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RfDialogMultiActionButtonOptions"/> class with the specified action
    /// text.
    /// </summary>
    /// <param name="actionText">The text to be used for the button's display, action, and associated data.</param>
    public RfDialogMultiActionButtonOptions(string actionText)
    {
        Text = actionText;
        Action = actionText;
        Data = actionText;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RfDialogMultiActionButtonOptions"/> class with the specified action
    /// text and CSS class.
    /// </summary>
    /// <param name="actionText">The text displayed on the action button. Cannot be null or empty.</param>
    /// <param name="cssClass">The CSS class applied to the action button for styling purposes. Can be null or empty if no specific styling is required.</param>
    public RfDialogMultiActionButtonOptions(string actionText, string cssClass)
        : this(actionText)
    {
        CssClass = cssClass;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RfDialogMultiActionButtonOptions"/> class with the specified action
    /// text, CSS class, and placement.
    /// </summary>
    /// <param name="actionText">The text displayed on the action button. Cannot be null or empty.</param>
    /// <param name="cssClass">The CSS class applied to the action button for styling purposes. Can be null or empty if no specific styling is required.</param>
    /// <param name="placement">The placement of the button within the dialog. Default is <see cref="RfPlacement.Right"/>.</param>
    public RfDialogMultiActionButtonOptions(string actionText, string cssClass, RfPlacement placement)
        : this(actionText, cssClass)
    {
        Placement = placement;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RfDialogMultiActionButtonOptions"/> class with the specified action
    /// text and CSS class.
    /// </summary>
    /// <param name="actionText">The text displayed on the action button. Cannot be null or empty.</param>
    /// <param name="placement">The placement of the button within the dialog. Default is <see cref="RfPlacement.Right"/>.</param>
    public RfDialogMultiActionButtonOptions(string actionText, RfPlacement placement)
        : this(actionText)
    {
        Placement = placement;
    }

    /// <summary>
    /// Gets or sets the text content associated with this instance.
    /// </summary>
    public string Text { get; set; }
    /// <summary>
    /// Gets or sets the action to be performed. 
    /// </summary>
    public string Action { get; set; }
    /// <summary>
    /// Gets or sets the CSS class applied to the element.
    /// </summary>
    public string CssClass { get; set; } = "is-primary";

    /// <summary>
    /// Gets or sets a value indicating whether the action is disabled.
    /// </summary>
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the icon associated with the action.
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for the icon.
    /// </summary>
    /// <remarks>This property allows you to specify a CSS class for the icon, enabling customization of its appearance. 
    /// Use <see cref="Icon"/> to set the css class for the actual icon.</remarks>
    public string IconWrapperCss { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the icon should be displayed on the left side or right side of the button. true for left, false for right.
    /// </summary>
    public bool IconOnLeft { get; set; } = true;

    /// <summary>
    /// Gets or sets the placement of the button within the dialog. Default is <see cref="RfPlacement.Right"/>.
    /// </summary>
    public RfPlacement Placement { get; set; } = RfPlacement.Right;

    /// <summary>
    /// Gets or sets the data associated with the current action.
    /// </summary>
    public object Data { get; set; }
}