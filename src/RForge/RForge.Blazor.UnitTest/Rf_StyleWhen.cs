using RForgeBlazor;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class Rf_StyleWhen : VerifyBase
{
    [TestMethod]
    public Task IsEmpty()
    {
        return Verify(Rf.StyleWhen());
    }
    
    [TestMethod]
    public Task IsNull()
    {
        return Verify(Rf.StyleWhen(null));
    }

    [TestMethod]
    public Task LengthOfZero()
    {
        return Verify(Rf.StyleWhen(new (string styleName, string value, bool show)[0]));
    }

    [TestMethod]
    public Task ShowOneStyle()
    {
        return Verify(Rf.StyleWhen(("color", "red", true)));
    }

    [TestMethod]
    public Task ShowTwoStyle()
    {
        return Verify(Rf.StyleWhen(("color", "red", true), ("background-color", "#001100", true)));
    }

    [TestMethod]
    public Task TestLongStyle()
    {
        return Verify(Rf.StyleWhen(("uhynjactezacqhbcfpwapejwhmbbjekbakexigucapixhrhxqhaychiyyfbvfpnnhyuhppmcyuqjqhqtprgauieryzjpukhwkherktwvpkyzhptijjrudhidpfetfnxwjhurhhkgukcvbtwqpefanhkydhzctgfmdfwtepumujixyvxdxzujmcxqdayigrqazwyvhfppqqkpixkfhvkzunekkfzmykfadhwwkzzzfympikzggnrntmdjwcwftmzzngvurvverhtrjnqhaxavnntaunrrgfwctbmfpkwkwjqywxjfyykpwjaxfurbbdncfbccdvcaqhfcbbffeqcjkuzewgegndrzyiuxxayfvfmtubekzxxwwrrnxiqbzxqkvwkknurgwkrjtnmmmrvdrkmwnnpqzjwuiqqrnfyvmxgzqzcqjryakcfjwuvfcuvwddzpvwjivjtnayujbnemdzviuzudamaekuwkndgvhbfrzjwgpzbinvpbpceygqxa", (1m / 3m).ToString(), true)));
    }

    [TestMethod]
    public Task TestMultipleStyles()
    {
        List<(string, string, bool)> styles = new List<(string, string, bool)>();

        for (int i = 0; i < 100; i++)
            styles.Add(($"style" + i, i.ToString(), true));

        return Verify(Rf.StyleWhen(styles.ToArray()));
    }

    [TestMethod]
    public Task TestOneHide()
    {
        return Verify(Rf.StyleWhen(("color", "red", false)));
    }
    [TestMethod]
    public Task TestTwoHide()
    {
        return Verify(Rf.StyleWhen(("color", "red", false), ("color2", "orange", false)));
    }

    [TestMethod]
    public Task ShowOneHideTwo()
    {
        return Verify(Rf.StyleWhen(("color", "red", false), ("color", "orange", true), ("color", "yellow", false)));
    }

}