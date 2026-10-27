using Bunit;
using Microsoft.AspNetCore.Components;
using RForge.Abstractions.DropDowns;

using BunitTestContext = Bunit.TestContext;
using SingleDropDown = RForgeBlazor.RfDropDown<string>;
using MultiDropDown = RForgeBlazor.RfDropDownMulti<string>;

#pragma warning disable BL0005 // Component parameters set directly: intentional in bUnit test fixtures.
/// <summary>
/// Rendered-markup tests for #66, derived 1:1 from the spec §8 acceptance bullets
/// (issue #66). bUnit approved for the test project via issue #68 (§4, PO 2026-10-26).
/// These assert the OBSERVABLE class attribute of the rendered root div and filter
/// input — the public seam a consumer sees — not internal computeds.
/// </summary>
[TestClass]
public class RfDropDown_CssRendering : BunitTestContext
{
    private static ComponentParameter Param(string name, object value)
        => ComponentParameter.CreateParameter(name, value);

    private static string[] Classes(IRenderedComponent<SingleDropDown> cut, string selector)
        => cut.Find(selector).GetAttribute("class")!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // §8 bullet 1 (good path): RfDropDown with CssClass="foo" -> root has dropdown AND foo.
    [TestMethod]
    public void Given_SingleDropDown_When_SetCssClass_Then_RootHasDropdownAndFoo()
    {
        var cut = RenderComponent<SingleDropDown>(new[] { Param("CssClass", "foo") });

        var classes = Classes(cut, "div.dropdown");
        CollectionAssert.Contains(classes, "dropdown");
        CollectionAssert.Contains(classes, "foo");
    }

    // §8 bullet 2 (good path): RfDropDownMulti with CssClass="foo" -> same.
    [TestMethod]
    public void Given_MultiDropDown_When_SetCssClass_Then_RootHasDropdownAndFoo()
    {
        var cut = RenderComponent<MultiDropDown>(new[] { Param("CssClass", "foo") });

        var classes = cut.Find("div.dropdown").GetAttribute("class")!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.Contains(classes, "dropdown");
        CollectionAssert.Contains(classes, "foo");
    }

    // §8 bullet 3 (good path): ShowFilter + CssFilterClass="bar" -> filter input has input AND bar.
    [TestMethod]
    public void Given_SingleDropDownShowFilter_When_SetCssFilterClass_Then_InputHasInputAndBar()
    {
        var cut = RenderComponent<SingleDropDown>(new[]
        {
            Param("ShowFilter", true),
            Param("CssFilterClass", "bar"),
        });

        var classes = Classes(cut, "input[type=search]");
        CollectionAssert.Contains(classes, "input");
        CollectionAssert.Contains(classes, "bar");
    }

    // §8 bullet 3 for the Multi variant (good path): same bullet, both components.
    [TestMethod]
    public void Given_MultiDropDownShowFilter_When_SetCssFilterClass_Then_InputHasInputAndBar()
    {
        var cut = RenderComponent<MultiDropDown>(new[]
        {
            Param("ShowFilter", true),
            Param("CssFilterClass", "bar"),
        });

        var classes = cut.Find("input[type=search]").GetAttribute("class")!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.Contains(classes, "input");
        CollectionAssert.Contains(classes, "bar");
    }

    // §8 bullet 4 (boundary): params unset -> classes are exactly the pre-fix set.
    // Asserted as the exact class SET so a future addition anywhere in the merge is caught.
    [TestMethod]
    public void Given_ParamsUnset_When_Rendered_Then_ClassListsMatchMain()
    {
        var cut = RenderComponent<SingleDropDown>(new[] { Param("ShowFilter", true) });

        CollectionAssert.AreEqual(
            new[] { "dropdown" },
            Classes(cut, "div.dropdown").OrderBy(c => c).ToArray(),
            "Root class list drifted with params unset.");
        CollectionAssert.AreEqual(
            new[] { "input" },
            Classes(cut, "input[type=search]").OrderBy(c => c).ToArray(),
            "Filter input class list drifted with params unset.");
    }

    // §8 bullet 4 boundary, bad-path edge: whitespace-only values must not add classes
    // and must not leave stray double spaces in the attribute (spec §6 gotcha).
    [TestMethod]
    public void Given_WhitespaceOnlyCss_When_Rendered_Then_NoStrayClassesOrSpaces()
    {
        var cut = RenderComponent<SingleDropDown>(new[]
        {
            Param("CssClass", "   "),
            Param("CssFilterClass", string.Empty),
            Param("ShowFilter", true),
        });

        // Token-set equality, not string equality: the razor template's literal
        // `class="dropdown @DropdownCss"` leaves a trailing space on main too when
        // DropdownCss is empty — pre-existing template shape, not part of the fix.
        // The real §6 gotcha is double-spaces/empty tokens from a sloppy merge.
        CollectionAssert.AreEqual(new[] { "dropdown" }, Classes(cut, "div.dropdown"));
        CollectionAssert.AreEqual(new[] { "input" }, Classes(cut, "input[type=search]"));

        // Internal double spaces WOULD be a merge bug (token-split above would hide it):
        var rootAttr = cut.Find("div.dropdown").GetAttribute("class")!;
        var inputAttr = cut.Find("input[type=search]").GetAttribute("class")!;
        Assert.IsFalse(rootAttr.Trim().Contains("  "), "Stray double space in root class attr.");
        Assert.IsFalse(inputAttr.Trim().Contains("  "), "Stray double space in filter class attr.");
    }

    // §8 bullet 5 (good path): IsOpen + Position + CssClass -> is-active, position, user class coexist.
    [TestMethod]
    public void Given_OpenRightUpDropDownWithCssClass_When_Rendered_Then_AllClassesCoexist()
    {
        var cut = RenderComponent<SingleDropDown>(new[]
        {
            Param("CssClass", "foo"),
            Param("IsOpen", true),
            Param("IsReadonly", false),
            Param("Position", RfDropDownPosition.RightUp),
        });

        var classes = Classes(cut, "div.dropdown");
        CollectionAssert.Contains(classes, "is-active");
        CollectionAssert.Contains(classes, "is-right");
        CollectionAssert.Contains(classes, "is-up");
        CollectionAssert.Contains(classes, "foo");
    }

    // §8 bullet 6 (regression): CssSelectedClass / CssRowItemClass still reach their elements.
    [TestMethod]
    public void Given_SelectedClassAndRowClass_When_RenderedOpen_Then_StillApplied()
    {
        RenderFragment<string> text = item => builder => builder.AddContent(0, item);
        var cut = RenderComponent<SingleDropDown>(new[]
        {
            Param("Items", new List<string> { "a", "b" }),
            Param("SelectedItem", "a"),
            Param("IsOpen", true),
            Param("CssSelectedClass", "sel-cls"),
            Param("CssRowItemClass", "row-cls"),
            Param("ChildContent", text),
            Param("RowItemTemplate", text),
        });

        Assert.IsNotNull(cut.Find("span.sel-cls"));
        Assert.IsTrue(cut.FindAll("a.row-cls").Count >= 1, "CssRowItemClass no longer reaches row items.");
    }
}
