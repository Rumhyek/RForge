namespace RForgeBlazor.Services
{
    internal static class CssHelper
    {

        /// <summary>
        /// A simple helper to add <paramref name="addClass"/> to <paramref name="css"/> if <paramref name="shouldAdd"/> equals true.
        /// </summary>
        /// <param name="css">The current css string.</param>
        /// <param name="shouldAdd">A true false value. On true add the add class to css.</param>
        /// <param name="addClass">The text to add.</param>
        internal static void AddIfTrue(ref string css, bool shouldAdd, string addClass)
        {
            if (shouldAdd == false) return;

            if (css == null)
                css = addClass;
            else
                css += $" {addClass}";
        }

        /// <summary>
        /// A simple helper to add <paramref name="addClass"/> to <paramref name="css"/> if <paramref name="shouldAdd"/> equals true.
        /// </summary>
        /// <param name="css">The current css string.</param>
        /// <param name="shouldAdd">A true false value. On true add the add class to css.</param>
        /// <param name="addClass">The text to add called only if shouldAdd == true.</param>
        internal static void AddIfTrue(ref string css, bool shouldAdd, Func<string> classToAdd)
        {
            if (shouldAdd == false) return;

            if (css == null)
                css = classToAdd();
            else
                css += $" {classToAdd()}";
        }

        /// <summary>
        /// A simple inline if statmenets that returns <paramref name="addClass"/> or null based on <paramref name="shouldAdd"/>
        /// </summary>
        /// <param name="shouldAdd">If true returns <paramref name="addClass"/></param>
        /// <param name="addClass">The class to add.</param>
        /// <returns></returns>
        internal static string AddIf(bool shouldAdd, string addClass)
        {
            if (shouldAdd == false) return null;

            return addClass;
        }
    }
}
