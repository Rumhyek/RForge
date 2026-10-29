using BenchmarkDotNet.Attributes;
using RForgeBlazor;
using System.Xml.Serialization;

[MemoryDiagnoser]
// Real Blazor runs Workstation GC; keep the measured job on Workstation so
// allocation costs are not under-penalized the way Server GC defaults hide them (#114).
[GcServer(false)]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_ClassWhen_One_Benchmark
{
    // Single-entry scale shapes: empty, single visible, single hidden, small
    // mixes. Entry 0 keeps the original hardcoded row so the default shape
    // reproduces the pre-#114 measurement. The repeated "class-a" makes the
    // HashSet-based BootstrapBlazor baseline dedup where Rf passes through.
    private static readonly string[] classShape = [
        "class-a",
        "class-a",
        "class-b",
    ];

    [Params(0, 1, 2)]
    public int EntryCount = 1;

    // Trailing entries hidden; covers the hidden fast path and the all-hidden
    // early return without exploding the run matrix.
    [Params(0, 1)]
    public int HiddenCount = 0;

    private (string className, bool show)[] benchmarkTest = [];

    [GlobalSetup]
    public void Setup()
    {
        int hidden = Math.Min(HiddenCount, EntryCount);
        var data = new (string className, bool show)[EntryCount];

        for (int i = 0; i < EntryCount; i++)
        {
            data[i] = (classShape[i % classShape.Length], show: i < EntryCount - hidden);
        }

        benchmarkTest = data;
    }

    [Benchmark(Baseline = true)]
    public string BootstrapBlazorMethod() => BootstrapBlazor.ClassWhen(benchmarkTest);

    [Benchmark]
    public string ForeachMethod() => RfBasicForeach.ClassWhen(benchmarkTest);

    [Benchmark]
    public string RfMethod() => Rf.ClassWhen(benchmarkTest);


    public static void PrintValues()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(nameof(Rf_ClassWhen_One_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        var shape = new Rf_ClassWhen_One_Benchmark();
        shape.Setup();

        Console.WriteLine(nameof(BootstrapBlazor));
        Console.WriteLine(BootstrapBlazor.ClassWhen(shape.benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfBasicForeach));
        Console.WriteLine(RfBasicForeach.ClassWhen(shape.benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.ClassWhen(shape.benchmarkTest));
        Console.WriteLine();
    }


}
