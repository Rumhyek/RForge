using BenchmarkDotNet.Attributes;
using RForgeBlazor;
using System.Xml.Serialization;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_ClassWhen_One_Benchmark
{
    public static readonly (string className, bool show)[] benchmarkTest = [
        ("class-a", true),
    ];

    [Benchmark(Baseline = true)]
    public void BootstrapBlazorMethod() => BootstrapBlazor.ClassWhen(benchmarkTest);

    [Benchmark]
    public void ForeachMethod() => RfBasicForeach.ClassWhen(benchmarkTest);

    [Benchmark]
    public void RfMethod() => Rf.ClassWhen(benchmarkTest);


    public static void PrintValues()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(nameof(Rf_ClassWhen_One_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        Console.WriteLine(nameof(BootstrapBlazor));
        Console.WriteLine(BootstrapBlazor.ClassWhen(benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfBasicForeach));
        Console.WriteLine(RfBasicForeach.ClassWhen(benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.ClassWhen(benchmarkTest));
        Console.WriteLine();
    }


}
