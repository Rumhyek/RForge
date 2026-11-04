namespace RForgeDocs.Client.EventsExamples.Theming;

/// <summary>
/// Event-aware writer. It writes to whichever ThemeState provider is nearest: the root, or an enclosing StateScope.
/// </summary>
public partial class ThemePicker
{
    private static readonly ThemeState[] Themes = [ThemeState.Light, ThemeState.Dark, ThemeState.Ocean];

    private int _renders;

    [UseState(Select = nameof(SelectName))]
    private partial string Current { get; }

    private static string SelectName(ThemeState theme) => theme.Name;

    private void Pick(ThemeState theme) => State<ThemeState>().Set(theme);
}
