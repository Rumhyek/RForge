using RForgeBlazor;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class Rf_Class : VerifyBase
{
    [TestMethod]
    public Task IsEmpty()
    {
        return Verify(Rf.Class());
    }

    [TestMethod]
    public Task IsNull()
    {
        return Verify(Rf.Class(null));
    }

    [TestMethod]
    public Task LengthOfZero()
    {
        return Verify(Rf.Class(new string[0]));
    }

    [TestMethod]
    public Task ShowOneClass()
    {
        return Verify(Rf.Class("class"));
    }

    [TestMethod]
    public Task TestLongClassName()
    {
        return Verify(Rf.Class("uhynjactezacqhbcfpwapejwhmbbjekbakexigucapixhrhxqhaychiyyfbvfpnnhyuhppmcyuqjqhqtprgauieryzjpukhwkherktwvpkyzhptijjrudhidpfetfnxwjhurhhkgukcvbtwqpefanhkydhzctgfmdfwtepumujixyvxdxzujmcxqdayigrqazwyvhfppqqkpixkfhvkzunekkfzmykfadhwwkzzzfympikzggnrntmdjwcwftmzzngvurvverhtrjnqhaxavnntaunrrgfwctbmfpkwkwjqywxjfyykpwjaxfurbbdncfbccdvcaqhfcbbffeqcjkuzewgegndrzyiuxxayfvfmtubekzxxwwrrnxiqbzxqkvwkknurgwkrjtnmmmrvdrkmwnnpqzjwuiqqrnfyvmxgzqzcqjryakcfjwuvfcuvwddzpvwjivjtnayujbnemdzviuzudamaekuwkndgvhbfrzjwgpzbinvpbpceygqxa"));
    }

    [TestMethod]
    public Task TestMultipleClasses()
    {
        List<string> classes = new List<string>();

        for (int i = 0; i < 100; i++)
            classes.Add($"class{i}");

        return Verify(Rf.Class(classes.ToArray()));
    }
}
