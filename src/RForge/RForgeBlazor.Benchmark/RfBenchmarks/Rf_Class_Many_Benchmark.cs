using BenchmarkDotNet.Attributes;
using RForgeBlazor;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_Class_Many_Benchmark
{
    public static readonly string[] benchmarkTest = [
        "class-a",
        "class-b",
        "class-c",
        "class-d",
        "class-e",
        "class-f",
    ];

    [Benchmark(Baseline = true)]
    public void ForeachMethod() => RfBasicForeach.Class(benchmarkTest);

    [Benchmark]
    public void RfMethod() => Rf.Class(benchmarkTest);


    public static void PrintValues()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(nameof(Rf_Class_Many_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        Console.WriteLine(nameof(RfBasicForeach));
        Console.WriteLine(RfBasicForeach.Class(benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.Class(benchmarkTest));
        Console.WriteLine();
    }
}
