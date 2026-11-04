using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class RfKey_Normalize
{
    [TestMethod]
    [DataRow("s", "s")]
    [DataRow("S", "s")]
    [DataRow("?", "?")]
    [DataRow("esc", "escape")]
    [DataRow("Escape", "escape")]
    [DataRow("mod+s", "mod+s")]
    [DataRow("control+s", "ctrl+s")]
    [DataRow(" Ctrl + S ", "ctrl+s")]
    [DataRow("shift+alt+ctrl+mod+k", "ctrl+alt+shift+mod+k")]
    [DataRow("ctrl+ctrl+k", "ctrl+k")]
    [DataRow("up", "arrowup")]
    [DataRow("ctrl+plus", "ctrl+plus")]
    [DataRow("f12", "f12")]
    public void Given_ValidCombo_When_Normalized_Then_WrittenOneWay(string combo, string expected)
    {
        Assert.AreEqual(expected, RfKey.Combo(combo));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("ctrl")]
    [DataRow("ctrl+shift")]
    [DataRow("ctrl+")]
    [DataRow("ctrl++")]
    [DataRow("a+b")]
    [DataRow("ctrl+notakey")]
    public void Given_InvalidCombo_When_Normalized_Then_Throws(string combo)
    {
        _ = Assert.ThrowsException<ArgumentException>(() => RfKey.Combo(combo));
    }

    [TestMethod]
    public void Given_Parts_When_Combo_Then_JoinedAndNormalized()
    {
        Assert.AreEqual("shift+mod+k", RfKey.Combo(RfKey.Mod, RfKey.Shift, "K"));
        Assert.AreEqual("escape", RfKey.Combo(RfKey.Escape));
    }

    [TestMethod]
    public void Given_NoParts_When_Combo_Then_Throws()
    {
        _ = Assert.ThrowsException<ArgumentException>(() => RfKey.Combo());
        _ = Assert.ThrowsException<ArgumentException>(() => RfKey.Combo(null));
    }

    [TestMethod]
    public void Given_EveryConstant_When_Normalized_Then_Unchanged()
    {
        foreach (var field in typeof(RfKey).GetFields())
        {
            string value = (string)field.GetRawConstantValue();

            bool isModifier = value is RfKey.Ctrl or RfKey.Alt or RfKey.Shift or RfKey.Mod;
            string combo = isModifier ? value + "+k" : value;

            Assert.AreEqual(combo, RfKey.Combo(combo), field.Name);
        }
    }
}
