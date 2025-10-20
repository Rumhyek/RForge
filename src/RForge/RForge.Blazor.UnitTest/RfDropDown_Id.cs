using RForgeBlazor;
using RForgeBlazor.Models;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class RfDropDown_Id : VerifyBase
{
    [TestMethod]
    public void DropdownId_ShouldBeUnique()
    {
        // Arrange
        var dropdown1 = new RfDropDown<string>();
        var dropdown2 = new RfDropDown<string>();

        // Access the protected DropdownId property via reflection for testing
        var dropdownIdProperty = typeof(RfDropDownBase<string>).GetProperty("DropdownId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var id1 = dropdownIdProperty?.GetValue(dropdown1) as string;
        var id2 = dropdownIdProperty?.GetValue(dropdown2) as string;

        // Assert
        Assert.IsNotNull(id1);
        Assert.IsNotNull(id2);
        Assert.AreNotEqual(id1, id2);
        Assert.IsTrue(id1.StartsWith("dropdown-menu-"));
        Assert.IsTrue(id2.StartsWith("dropdown-menu-"));
    }

    [TestMethod]
    public void DropdownId_ShouldFollowCorrectFormat()
    {
        // Arrange
        var dropdown = new RfDropDown<string>();

        // Access the protected DropdownId property via reflection for testing
        var dropdownIdProperty = typeof(RfDropDownBase<string>).GetProperty("DropdownId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var id = dropdownIdProperty?.GetValue(dropdown) as string;

        // Assert
        Assert.IsNotNull(id);
        Assert.IsTrue(id.StartsWith("dropdown-menu-"));
        
        // Extract the GUID part and verify it's a valid GUID format
        var guidPart = id.Substring("dropdown-menu-".Length);
        Assert.IsTrue(Guid.TryParse(guidPart, out _));
    }

    [TestMethod]
    public void DropdownMultiId_ShouldBeUnique()
    {
        // Arrange
        var dropdown1 = new RfDropDownMulti<string>();
        var dropdown2 = new RfDropDownMulti<string>();

        // Access the protected DropdownId property via reflection for testing
        var dropdownIdProperty = typeof(RfDropDownBase<string>).GetProperty("DropdownId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var id1 = dropdownIdProperty?.GetValue(dropdown1) as string;
        var id2 = dropdownIdProperty?.GetValue(dropdown2) as string;

        // Assert
        Assert.IsNotNull(id1);
        Assert.IsNotNull(id2);
        Assert.AreNotEqual(id1, id2);
        Assert.IsTrue(id1.StartsWith("dropdown-menu-"));
        Assert.IsTrue(id2.StartsWith("dropdown-menu-"));
    }
}