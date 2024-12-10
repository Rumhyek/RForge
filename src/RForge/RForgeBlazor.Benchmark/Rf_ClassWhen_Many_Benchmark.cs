using BenchmarkDotNet.Attributes;
using RForgeBlazor;
using System.Xml.Serialization;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_ClassWhen_Many_Benchmark
{
    public static readonly (string className, bool show)[] benchmarkTest = [
        ("class-a", true),
        ("class-b", true),
        ("class-c", true),
        ("class-d", true),
        ("class-e", true),
        ("class-f", true),
        ("class-g", true),
        ("class-h", true),
        ("class-i", false),
        ("class-j", true),
        ("class-k", true),
        ("class-l", true),
        ("class-m", true),
        ("class-n", true),
        ("class-o", true),
        ("class-p", true),
        ("class-q", true),
    ];

    [Benchmark(Baseline = true)]
    public void BootstrapBlazorMethod() => BootstrapBlazor.ClassWhen(benchmarkTest);

    [Benchmark]
    public void ForeachMethod() => BasicForeach.ClassWhen(benchmarkTest);

    [Benchmark]
    public void RfMethod() => Rf.ClassWhen(benchmarkTest);


    public static void PrintValues()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(nameof(Rf_ClassWhen_Many_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        Console.WriteLine(nameof(BootstrapBlazor));
        Console.WriteLine(BootstrapBlazor.ClassWhen(benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(BasicForeach));
        Console.WriteLine(BasicForeach.ClassWhen(benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.ClassWhen(benchmarkTest));
        Console.WriteLine();
    }


}
