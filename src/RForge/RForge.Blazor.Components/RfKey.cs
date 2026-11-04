namespace RForge.Blazor;

/// <summary>
/// The modifiers and named keys used by <see cref="RfHotKey"/>, and a way to build a combo from them.
/// Letters, digits and symbols are written as themselves: <c>"s"</c>, <c>"1"</c>, <c>"?"</c>.
/// </summary>
/// <example>
/// <code>
/// &lt;RfHotKey Keys="@RfKey.Escape" OnPressed="Close" /&gt;
/// &lt;RfHotKey Keys="@RfKey.Combo(RfKey.Mod, RfKey.Shift, "k")" OnPressed="OpenSearch" /&gt;
/// &lt;RfHotKey Keys="mod+s" OnPressed="Save" /&gt;
/// </code>
/// </example>
public static class RfKey
{
    #region Modifiers
    /// <summary>The Ctrl key.</summary>
    public const string Ctrl = "ctrl";
    /// <summary>The Alt key, or Option on a Mac.</summary>
    public const string Alt = "alt";
    /// <summary>The Shift key.</summary>
    public const string Shift = "shift";
    /// <summary>Cmd on a Mac and Ctrl everywhere else.</summary>
    public const string Mod = "mod";
    #endregion

    #region Named keys
    /// <summary>The Esc key.</summary>
    public const string Escape = "escape";
    /// <summary>The Enter key.</summary>
    public const string Enter = "enter";
    /// <summary>The Tab key.</summary>
    public const string Tab = "tab";
    /// <summary>The space bar.</summary>
    public const string Space = "space";
    /// <summary>The Backspace key.</summary>
    public const string Backspace = "backspace";
    /// <summary>The Delete key.</summary>
    public const string Delete = "delete";
    /// <summary>The up arrow.</summary>
    public const string ArrowUp = "arrowup";
    /// <summary>The down arrow.</summary>
    public const string ArrowDown = "arrowdown";
    /// <summary>The left arrow.</summary>
    public const string ArrowLeft = "arrowleft";
    /// <summary>The right arrow.</summary>
    public const string ArrowRight = "arrowright";
    /// <summary>The Home key.</summary>
    public const string Home = "home";
    /// <summary>The End key.</summary>
    public const string End = "end";
    /// <summary>The Page Up key.</summary>
    public const string PageUp = "pageup";
    /// <summary>The Page Down key.</summary>
    public const string PageDown = "pagedown";
    /// <summary>The + key. Combos are joined with +, so it needs a name.</summary>
    public const string Plus = "plus";
    /// <summary>The F1 key.</summary>
    public const string F1 = "f1";
    /// <summary>The F2 key.</summary>
    public const string F2 = "f2";
    /// <summary>The F3 key.</summary>
    public const string F3 = "f3";
    /// <summary>The F4 key.</summary>
    public const string F4 = "f4";
    /// <summary>The F5 key.</summary>
    public const string F5 = "f5";
    /// <summary>The F6 key.</summary>
    public const string F6 = "f6";
    /// <summary>The F7 key.</summary>
    public const string F7 = "f7";
    /// <summary>The F8 key.</summary>
    public const string F8 = "f8";
    /// <summary>The F9 key.</summary>
    public const string F9 = "f9";
    /// <summary>The F10 key.</summary>
    public const string F10 = "f10";
    /// <summary>The F11 key.</summary>
    public const string F11 = "f11";
    /// <summary>The F12 key.</summary>
    public const string F12 = "f12";
    #endregion

    private static readonly string[] Modifiers = [Ctrl, Alt, Shift, Mod];

    private static readonly HashSet<string> NamedKeys =
    [
        Escape, Enter, Tab, Space, Backspace, Delete,
        ArrowUp, ArrowDown, ArrowLeft, ArrowRight,
        Home, End, PageUp, PageDown, Plus,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    ];

    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["esc"] = Escape,
        ["return"] = Enter,
        ["del"] = Delete,
        ["control"] = Ctrl,
        ["up"] = ArrowUp,
        ["down"] = ArrowDown,
        ["left"] = ArrowLeft,
        ["right"] = ArrowRight
    };

    /// <summary>
    /// Builds a combo from its parts.
    /// </summary>
    /// <param name="parts">Any modifiers and one key.</param>
    /// <returns>The combo, such as <c>mod+shift+k</c>.</returns>
    /// <exception cref="ArgumentException">The parts don't make a valid combo.</exception>
    public static string Combo(params string[] parts)
    {
        if (parts == null || parts.Length == 0)
        {
            throw new ArgumentException("A hot key combo needs a key.", nameof(parts));
        }

        return Normalize(string.Join('+', parts));
    }

    /// <summary>
    /// Validates a combo and writes it one way: lower case, no aliases, modifiers in a fixed order.
    /// </summary>
    /// <param name="combo">The combo to normalize.</param>
    /// <returns>The normalized combo.</returns>
    /// <exception cref="ArgumentException">The combo isn't valid.</exception>
    internal static string Normalize(string combo)
    {
        if (string.IsNullOrWhiteSpace(combo) == true)
        {
            throw new ArgumentException("A hot key combo needs a key.", nameof(combo));
        }

        HashSet<string> modifiers = new();
        string key = null;

        foreach (string rawPart in combo.Split('+'))
        {
            string part = rawPart.Trim().ToLowerInvariant();

            if (part.Length == 0)
            {
                throw new ArgumentException($"'{combo}' has an empty part. Use {nameof(RfKey)}.{nameof(Plus)} for the + key.", nameof(combo));
            }

            part = Aliases.GetValueOrDefault(part, part);

            if (Modifiers.Contains(part) == true)
            {
                modifiers.Add(part);
            }
            else if (key != null)
            {
                throw new ArgumentException($"'{combo}' has more than one key.", nameof(combo));
            }
            else if (part.Length == 1 || NamedKeys.Contains(part) == true)
            {
                key = part;
            }
            else
            {
                throw new ArgumentException($"'{combo}' has an unknown key: '{part}'.", nameof(combo));
            }
        }

        if (key == null)
        {
            throw new ArgumentException($"'{combo}' has no key, only modifiers.", nameof(combo));
        }

        return string.Join('+', Modifiers.Where(modifiers.Contains).Append(key));
    }
}
