using BenchmarkDotNet.Attributes;
using RForgeBlazor;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_StyleWhen_Many_Benchmark
{
    public static readonly (string styleName, string value, bool show)[] benchmarkTest = [
        ("color", "#011100", true),
        ("color", "#011000", true),
        ("color", "#010100", true),
        ("color", "#000000", true),
        ("color", "#001100", true),
        ("color", "#001000", false),
        ("color", "#111100", true),
        ("color", "#111000", true),
        ("color", "#110100", true),
        ("color", "#100000", true),
        ("color", "#101100", true),
        ("color", "#101000", true),
    ];


    [Benchmark(Baseline = true)]
    public void ForeachMethod() => RfBasicForeach.StyleWhen(benchmarkTest);

    [Benchmark]
    public void RfMethod() => Rf.StyleWhen(benchmarkTest);


    public static void PrintValues()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(nameof(Rf_ClassWhen_One_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        Console.WriteLine(nameof(RfBasicForeach));
        Console.WriteLine(RfBasicForeach.StyleWhen(benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.StyleWhen(benchmarkTest));
        Console.WriteLine();
    }
}