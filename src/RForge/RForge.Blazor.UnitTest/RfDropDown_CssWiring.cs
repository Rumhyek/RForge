using System.Reflection;
using RForge.Abstractions.DropDowns;
using RForgeBlazor;
using RForgeBlazor.Models;

namespace RForge.Blazor.UnitTest;

#pragma warning disable BL0005 // Component parameters set directly: intentional in plain-object unit tests.
/// <summary>
/// Smoke tests for #66: the CssClass / CssFilterClass parameters must reach the
/// rendered class lists via the base-class computeds (full rendered-markup tests
/// land with the test plan in the review/test lanes).
/// </summary>
[TestClass]
public class RfDropDown_CssWiring
{
    private const BindingFlags Protected = BindingFlags.NonPublic | BindingFlags.Instance;

    private static string GetComputed(RfDropDownBase<string> dropdown, string name)
        => dropdown.GetType()
            .GetProperty(name, Protected)
            ?.GetValue(dropdown) as string;

    [TestMethod]
    public void DropdownCss_IncludesUserCssClass()
    {
        // Arrange
        var dropdown = new RfDropDown<string> { CssClass = "foo" };

        // Act
        var css = GetComputed(dropdown, "DropdownCss");

        // Assert — acceptance bullet 1/2: user class rides alongside the dropdown classes
        Assert.IsNotNull(css);
        StringAssert.Contains(css, "foo");
    }

    [TestMethod]
    public void DropdownCss_CoexistsWithIsActiveAndPosition()
    {
        // Arrange — acceptance bullet 5: is-active + position + user class coexist
        var dropdown = new RfDropDown<string>
        {
            CssClass = "foo",
            IsOpen = true,
            IsReadonly = false,
            Position = RfDropDownPosition.RightUp,
        };

        // Act
        var css = GetComputed(dropdown, "DropdownCss");

        // Assert
        Assert.IsNotNull(css);
        StringAssert.Contains(css, "is-active");
        StringAssert.Contains(css, "is-right");
        StringAssert.Contains(css, "is-up");
        StringAssert.Contains(css, "foo");
    }

    [TestMethod]
    public void DropdownCss_UnchangedWhenCssClassUnset()
    {
        // Arrange — acceptance bullet 4: unset params keep markup identical to before

        // Act
        var css = GetComputed(new RfDropDown<string>(), "DropdownCss");

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(css));
    }

    [TestMethod]
    public void FilterInputCss_DefaultsToInputWhenCssFilterClassUnset()
    {
        // Assert
        Assert.AreEqual("input", GetComputed(new RfDropDown<string>(), "FilterInputCss"));
    }

    [TestMethod]
    public void FilterInputCss_IncludesUserCssFilterClass()
    {
        // Arrange
        var dropdown = new RfDropDown<string> { CssFilterClass = "bar" };

        // Act
        var css = GetComputed(dropdown, "FilterInputCss");

        // Assert — acceptance bullet 3: input AND bar
        Assert.IsNotNull(css);
        StringAssert.Contains(css, "input");
        StringAssert.Contains(css, "bar");
    }

    [TestMethod]
    public void FilterInputCss_SameForMultiVariant()
    {
        // Arrange — the fix lives in the base so both variants share it
        var dropdown = new RfDropDownMulti<string> { CssFilterClass = "bar" };

        // Act
        var css = GetComputed(dropdown, "FilterInputCss");

        // Assert
        StringAssert.Contains(css, "bar");
    }
}
