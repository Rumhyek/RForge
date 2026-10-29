using BenchmarkDotNet.Attributes;
using RForgeBlazor;

[MemoryDiagnoser]
// Real Blazor runs Workstation GC; keep the measured job on Workstation so
// allocation costs are not under-penalized the way Server GC defaults hide them (#114).
[GcServer(false)]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Rf_StyleWhen_Many_Benchmark
{
    // The original 12 hardcoded rows, kept as the value palette. All rows use
    // the same style name so larger counts cycle duplicates, and 48 pushes
    // the span buffer sizing further than the original fixed shape.
    private static readonly string[] styleValues = [
        "#011100",
        "#011000",
        "#010100",
        "#000000",
        "#001100",
        "#001000",
        "#111100",
        "#111000",
        "#110100",
        "#100000",
        "#101100",
        "#101000",
    ];

    [Params(3, 12, 48)]
    public int EntryCount = 12;

    // Trailing entries hidden (the original shape had one hidden row): covers
    // the skip-hidden path and the all-hidden early return.
    [Params(0, 1, 12)]
    public int HiddenCount = 1;

    private (string styleName, string value, bool show)[] benchmarkTest = [];

    [GlobalSetup]
    public void Setup()
    {
        int hidden = Math.Min(HiddenCount, EntryCount);
        var data = new (string styleName, string value, bool show)[EntryCount];

        for (int i = 0; i < EntryCount; i++)
        {
            data[i] = ("color", styleValues[i % styleValues.Length], show: i < EntryCount - hidden);
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
        Console.WriteLine(nameof(Rf_StyleWhen_Many_Benchmark));
        Console.WriteLine("--------------------------------------------------");

        var shape = new Rf_StyleWhen_Many_Benchmark();
        shape.Setup();

        Console.WriteLine(nameof(RfBasicForeach));
        Console.WriteLine(RfBasicForeach.StyleWhen(shape.benchmarkTest));
        Console.WriteLine();
        Console.WriteLine(nameof(RfMethod));
        Console.WriteLine(Rf.StyleWhen(shape.benchmarkTest));
        Console.WriteLine();
    }
}
