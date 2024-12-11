using RForgeBlazor;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class Rf_ClassWhen : VerifyBase
{
    [TestMethod]
    public Task IsEmpty()
    {
        return Verify(Rf.ClassWhen());
    }

    [TestMethod]
    public Task IsNull()
    {
        return Verify(Rf.ClassWhen(null));
    }

    [TestMethod]
    public Task LengthOfZero()
    {
        return Verify(Rf.ClassWhen(new (string cssClass, bool show)[0]));
    }

    [TestMethod]
    public Task ShowOneClass()
    {
        return Verify(Rf.ClassWhen(("class", true)));
    }

    [TestMethod]
    public Task ShowTwoClasses()
    {
        return Verify(Rf.ClassWhen(("class", true), ("class2", true)));
    }

    [TestMethod]
    public Task TestLongClassName()
    {
        return Verify(Rf.ClassWhen(("uhynjactezacqhbcfpwapejwhmbbjekbakexigucapixhrhxqhaychiyyfbvfpnnhyuhppmcyuqjqhqtprgauieryzjpukhwkherktwvpkyzhptijjrudhidpfetfnxwjhurhhkgukcvbtwqpefanhkydhzctgfmdfwtepumujixyvxdxzujmcxqdayigrqazwyvhfppqqkpixkfhvkzunekkfzmykfadhwwkzzzfympikzggnrntmdjwcwftmzzngvurvverhtrjnqhaxavnntaunrrgfwctbmfpkwkwjqywxjfyykpwjaxfurbbdncfbccdvcaqhfcbbffeqcjkuzewgegndrzyiuxxayfvfmtubekzxxwwrrnxiqbzxqkvwkknurgwkrjtnmmmrvdrkmwnnpqzjwuiqqrnfyvmxgzqzcqjryakcfjwuvfcuvwddzpvwjivjtnayujbnemdzviuzudamaekuwkndgvhbfrzjwgpzbinvpbpceygqxa", true)));
    }


    [TestMethod]
    public Task TestMultipleClasses()
    {
        List<(string, bool)> classes = new List<(string, bool)>();

        for (int i = 0; i < 100; i++)
            classes.Add(($"class" + i, true));

        return Verify(Rf.ClassWhen(classes.ToArray()));
    }

    [TestMethod]
    public Task TestOneClassHide()
    {
        return Verify(Rf.ClassWhen(("class", false)));
    }

    [TestMethod]
    public Task TestTwoClassHide()
    {
        return Verify(Rf.ClassWhen(("class", false), ("class2", false)));
    }

    [TestMethod]
    public Task TestThreeClassOneHideTwoShow()
    {
        return Verify(Rf.ClassWhen(("show", true), ("hide", false), ("show2", true)));
    }

    [TestMethod]
    public Task TestThreeClassTwoHideOneShow()
    {
        return Verify(Rf.ClassWhen(("hide", false), ("show", true), ("hide2", false)));
    }
}
