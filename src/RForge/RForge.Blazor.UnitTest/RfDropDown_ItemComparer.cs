using RForgeBlazor;
using RForgeBlazor.Models;
using System.Reflection;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class RfDropDown_ItemComparer : VerifyBase
{
    private Func<string, string, bool> GetDefaultItemComparer(RfDropDownBase<string> dropdown)
    {
        // Access the private _defaultItemComparer field via reflection for testing
        var defaultItemComparerField = typeof(RfDropDownBase<string>).GetField("_defaultItemComparer", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        return defaultItemComparerField?.GetValue(dropdown) as Func<string, string, bool>;
    }

    [TestMethod]
    public void DefaultItemComparer_ShouldHandleNullValues()
    {
        // Arrange
        var dropdown = new RfDropDown<string>();
        var comparer = GetDefaultItemComparer(dropdown);

        // Assert
        Assert.IsNotNull(comparer);
        
        // Test null comparisons
        Assert.IsTrue(comparer(null, null)); // null equals null
        Assert.IsFalse(comparer("test", null)); // non-null does not equal null
        Assert.IsFalse(comparer(null, "test")); // null does not equal non-null
    }

    [TestMethod]
    public void DefaultItemComparer_ShouldHandleNormalValues()
    {
        // Arrange
        var dropdown = new RfDropDown<string>();
        var comparer = GetDefaultItemComparer(dropdown);

        // Assert
        Assert.IsNotNull(comparer);
        
        // Test normal value comparisons
        Assert.IsTrue(comparer("test", "test")); // same strings
        Assert.IsFalse(comparer("test", "other")); // different strings
        Assert.IsTrue(comparer("", "")); // empty strings
    }

    [TestMethod]
    public void DefaultItemComparer_ShouldHandleIntegerValues()
    {
        // Arrange
        var dropdown = new RfDropDown<int?>();
        
        // Access the private _defaultItemComparer field via reflection for testing
        var defaultItemComparerField = typeof(RfDropDownBase<int?>).GetField("_defaultItemComparer", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var comparer = defaultItemComparerField?.GetValue(dropdown) as Func<int?, int?, bool>;

        // Assert
        Assert.IsNotNull(comparer);
        
        // Test integer comparisons including nulls
        Assert.IsTrue(comparer(null, null)); // null equals null
        Assert.IsFalse(comparer(1, null)); // non-null does not equal null
        Assert.IsFalse(comparer(null, 1)); // null does not equal non-null
        Assert.IsTrue(comparer(42, 42)); // same integers
        Assert.IsFalse(comparer(42, 24)); // different integers
    }

    [TestMethod]
    public void DefaultItemComparer_ShouldBeAssignedToItemComparerByDefault()
    {
        // Arrange
        var dropdown = new RfDropDown<string>();
        
        // Trigger OnParametersSet by accessing a property that would cause it to be called
        // Since we can't directly call protected OnParametersSet, we simulate the scenario
        var itemComparerProperty = typeof(RfDropDownBase<string>).GetProperty("ItemComparer");
        
        // Act - simulate parameters being set (this would happen during component initialization)
        dropdown.GetType().GetMethod("OnParametersSet", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(dropdown, null);
        
        var itemComparer = itemComparerProperty?.GetValue(dropdown) as Func<string, string, bool>;

        // Assert
        Assert.IsNotNull(itemComparer);
        
        // Test that the assigned comparer handles null values robustly
        Assert.IsTrue(itemComparer(null, null));
        Assert.IsFalse(itemComparer("test", null));
        Assert.IsFalse(itemComparer(null, "test"));
        Assert.IsTrue(itemComparer("test", "test"));
    }
}