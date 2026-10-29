using BenchmarkDotNet.Attributes;
using RForgeBlazor;

[MemoryDiagnoser]
// Real Blazor runs Workstation GC; keep the measured job on Workstation so
// allocation costs are not under-penalized the way Server GC defaults hide them (#114).
[GcServer(false)]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_StyleWhen_One_Benchmark
{
    // Single-entry scale shapes: empty, single visible, single hidden, small
    // mixes. Entry 0 keeps the original hardcoded row so the default shape
    // reproduces the pre-#114 measurement.
    private static readonly (string styleName, string value)[] styleShape = [
        ("color", "#001100"),
        ("background-color", "#110011"),
        ("font-size", "1rem"),
    ];

    [Params(0, 1, 2)]
    public int EntryCount = 1;

    // Trailing entries hidden; covers the hidden fast path and the all-hidden
    // early return without exploding the run matrix.
    [Params(0, 1)]
    public int HiddenCount = 0;

    private (string styleName, string value, bool show)[] benchmarkTest = [];

    [GlobalSetup]
    public void Setup()
    {
        int hidden = Math.Min(HiddenCount, EntryCount);
        var data = new (string styleName, string value, bool show)[EntryCount];

        for (int i = 0; i < EntryCount; i++)
        {
            (string styleName, string value) = styleShape[i % styleShape.Length];
            data[i] = (styleName, value, show: i < EntryCount - hidden);
        }

        benchmarkTest = data;
    }

    [Benchmark(Baseline = true)]
    public string ForeachMethod() => RfBasicForeach.StyleWhen(benchmarkTest);

    [Benchmark]
    public string RfMethod() => Rf.StyleWhen(benchmarkTest);


    public static void PrintValues()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(nameof(Rf_StyleWhen_One_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        var shape = new Rf_StyleWhen_One_Benchmark();
        shape.Setup();

        Console.WriteLine(nameof(RfBasicForeach));
        Console.WriteLine(RfBasicForeach.StyleWhen(shape.benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.StyleWhen(shape.benchmarkTest));
        Console.WriteLine();
    }
}
