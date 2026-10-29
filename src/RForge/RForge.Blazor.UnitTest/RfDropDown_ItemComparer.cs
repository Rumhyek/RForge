using Bunit;
using Microsoft.AspNetCore.Components;

using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest;

/// <summary>
/// Behavior tests for the public ItemComparer seam of RfDropDown (#112, finding F43 of the
/// #68 runtime-bug sweep). The previous suite read the private _defaultItemComparer field
/// and invoked OnParametersSet via reflection, so a behavior-preserving refactor (rename,
/// lazy-getter fallback, moved assignment site) silently turned every test red while the
/// shipped behavior was unchanged. These tests drive the component through bUnit's normal
/// render lifecycle and assert only observable behavior: the public ItemComparer property
/// and the is-selected markup that selection rendering flows through it. No reflection.
/// </summary>
[TestClass]
public class RfDropDown_ItemComparer : BunitTestContext
{
    private static ComponentParameter Param(string name, object value)
        => ComponentParameter.CreateParameter(name, value);

    private static RenderFragment<string> TextFragment =>
        item => builder => builder.AddContent(0, item);

    // Does the row rendered for the given item text carry the is-selected class?
    // (The consumer-visible selection marker rendered by the component.)
    private static bool RowIsSelected(IRenderedFragment cut, string itemText)
    {
        var rows = cut.FindAll("a.dropdown-item")
            .Where(e => e.TextContent == itemText)
            .ToList();

        Assert.AreEqual(1, rows.Count, $"Expected exactly one row for item '{itemText}'.");

        return rows[0].GetAttribute("class")?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains("is-selected") == true;
    }

    // §8 bullet 1: a rendered dropdown with no consumer ItemComparer ends up with a usable
    // default on the public property (read through the public seam after the real Blazor
    // parameter lifecycle, not by reflecting into OnParametersSet).
    [TestMethod]
    public void Given_RenderedDropDownWithoutItemComparer_When_ReadItemComparer_Then_DefaultComparerIsAssigned()
    {
        var cut = RenderComponent<RForgeBlazor.RfDropDown<string>>();

        Assert.IsNotNull(cut.Instance.ItemComparer);
    }

    // §8 bullet 2: default comparer null semantics (was ShouldHandleNullValues).
    [TestMethod]
    public void Given_DefaultItemComparer_When_CompareNulls_Then_OnlyNullEqualsNull()
    {
        var cut = RenderComponent<RForgeBlazor.RfDropDown<string>>();
        var comparer = cut.Instance.ItemComparer;

        Assert.IsTrue(comparer(null, null)); // null equals null
        Assert.IsFalse(comparer("test", null)); // non-null does not equal null
        Assert.IsFalse(comparer(null, "test")); // null does not equal non-null
    }

    // §8 bullet 2: default comparer string value semantics (was ShouldHandleNormalValues).
    [TestMethod]
    public void Given_DefaultItemComparer_When_CompareStrings_Then_UsesValueEquality()
    {
        var cut = RenderComponent<RForgeBlazor.RfDropDown<string>>();
        var comparer = cut.Instance.ItemComparer;

        Assert.IsTrue(comparer("test", "test")); // same strings
        Assert.IsFalse(comparer("test", "other")); // different strings
        Assert.IsTrue(comparer("", "")); // empty strings
    }

    // §8 bullet 2: default comparer nullable-int semantics (was ShouldHandleIntegerValues).
    [TestMethod]
    public void Given_DefaultItemComparerForNullableInts_When_CompareValues_Then_UsesValueEqualityAcrossNull()
    {
        var cut = RenderComponent<RForgeBlazor.RfDropDown<int?>>();
        var comparer = cut.Instance.ItemComparer;

        Assert.IsTrue(comparer(null, null)); // null equals null
        Assert.IsFalse(comparer(1, null)); // non-null does not equal null
        Assert.IsFalse(comparer(null, 1)); // null does not equal non-null
        Assert.IsTrue(comparer(42, 42)); // same integers
        Assert.IsFalse(comparer(42, 24)); // different integers
    }

    // §8 bullet 2: default comparer honors Equals on custom objects
    // (was ShouldHandleCustomObjects).
    [TestMethod]
    public void Given_DefaultItemComparerForCustomObjects_When_CompareInstances_Then_UsesEqualsOverride()
    {
        var cut = RenderComponent<RForgeBlazor.RfDropDown<TestItem>>();
        var comparer = cut.Instance.ItemComparer;

        var item1 = new TestItem { Id = 1, Name = "Test" };
        var item2 = new TestItem { Id = 1, Name = "Test" }; // Same values, different instance
        var item3 = new TestItem { Id = 2, Name = "Other" };
        var item4 = item1; // Same reference

        Assert.IsTrue(comparer(null, null)); // null equals null
        Assert.IsFalse(comparer(item1, null)); // non-null does not equal null
        Assert.IsFalse(comparer(null, item1)); // null does not equal non-null
        Assert.IsTrue(comparer(item1, item2)); // objects with same values (using Equals override)
        Assert.IsFalse(comparer(item1, item3)); // objects with different values
        Assert.IsTrue(comparer(item1, item4)); // same reference
    }

    // §8 bullet 3: the consumer's comparer survives the render lifecycle; the default
    // never clobbers it (the old suite could not express this without touching the field).
    [TestMethod]
    public void Given_ConsumerSuppliedItemComparer_When_ComponentRenders_Then_ConsumerComparerIsKept()
    {
        Func<string, string, bool> caseInsensitive =
            (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        var cut = RenderComponent<RForgeBlazor.RfDropDown<string>>(
            Param("ItemComparer", caseInsensitive));

        Assert.AreSame(caseInsensitive, cut.Instance.ItemComparer); // kept, not replaced
        Assert.IsTrue(cut.Instance.ItemComparer("ABC", "abc")); // and it behaves as supplied
    }

    // §8 bullet 4: the default comparer drives what the component renders as selected.
    [TestMethod]
    public void Given_RenderedDropDownWithDefaultComparer_When_ItemEqualsSelectedItem_Then_OnlyMatchingRowIsSelected()
    {
        var cut = RenderComponent<RForgeBlazor.RfDropDown<string>>(new[]
        {
            Param("Items", new List<string> { "Item1", "Item2" }),
            Param("SelectedItem", "Item2"),
            Param("ChildContent", TextFragment),
        });

        Assert.IsTrue(RowIsSelected(cut, "Item2"));
        Assert.IsFalse(RowIsSelected(cut, "Item1"));
    }

    // §8 bullet 4: a consumer comparer drives selection math end to end (case-folded
    // selection highlights the row the default comparer would call different).
    [TestMethod]
    public void Given_RenderedDropDownWithCaseInsensitiveComparer_When_SelectionMatchesByCaseFold_Then_RowIsSelected()
    {
        Func<string, string, bool> caseInsensitive =
            (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        var cut = RenderComponent<RForgeBlazor.RfDropDown<string>>(new[]
        {
            Param("Items", new List<string> { "abc", "XYZ" }),
            Param("SelectedItem", "ABC"),
            Param("ItemComparer", caseInsensitive),
            Param("ChildContent", TextFragment),
        });

        Assert.IsTrue(RowIsSelected(cut, "abc")); // matched case-insensitively
        Assert.IsFalse(RowIsSelected(cut, "XYZ"));
    }

    // Test class for object comparison testing
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TestItem other)
            {
                return Id == other.Id && Name == other.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}
