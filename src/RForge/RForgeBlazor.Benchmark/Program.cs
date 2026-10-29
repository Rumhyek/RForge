using BenchmarkDotNet.Running;

// Dispatch table for the benchmark entry points. The old code was five
// un-else'd ifs: a typo'd or missing arg matched nothing and the process
// exited 0 having measured nothing (#114). Now exactly one recognized
// command is required, anything else prints usage and exits 1.
// Arguments after the command are forwarded to BenchmarkDotNet so runs can
// be scoped (e.g. --filter, --job) without editing code. The config slot is
// passed explicitly as null with a materialized array so BDN parses only the
// forwarded arguments and never re-reads the process command line (which
// still holds the command token).
var dispatch = new Dictionary<string, Action>(StringComparer.Ordinal)
{
    ["rf-class-when-many"] = () =>
    {
        Rf_ClassWhen_Many_Benchmark.PrintValues();
        BenchmarkRunner.Run<Rf_ClassWhen_Many_Benchmark>(null, Remaining(args));
    },
    ["rf-class-when-one"] = () =>
    {
        Rf_ClassWhen_One_Benchmark.PrintValues();
        BenchmarkRunner.Run<Rf_ClassWhen_One_Benchmark>(null, Remaining(args));
    },
    ["rf-style-when-many"] = () =>
    {
        Rf_StyleWhen_Many_Benchmark.PrintValues();
        BenchmarkRunner.Run<Rf_StyleWhen_Many_Benchmark>(null, Remaining(args));
    },
    ["rf-style-when-one"] = () =>
    {
        Rf_StyleWhen_One_Benchmark.PrintValues();
        BenchmarkRunner.Run<Rf_StyleWhen_One_Benchmark>(null, Remaining(args));
    },
    ["rf-class-many"] = () =>
    {
        Rf_Class_Many_Benchmark.PrintValues();
        BenchmarkRunner.Run<Rf_Class_Many_Benchmark>(null, Remaining(args));
    },
};

static string[] Remaining(string[] args) => args.Length > 1 ? args[1..] : [];

static bool IsSwitch(string token) => token.StartsWith("--", StringComparison.Ordinal);

static void Usage(string reason, Dictionary<string, Action> commands)
{
    Console.Error.WriteLine(reason);
    foreach (string command in commands.Keys)
    {
        Console.Error.WriteLine($"  {command}");
    }

    Console.Error.WriteLine("Optional trailing arguments are BenchmarkDotNet switches (--filter, --job, ...).");
}

if (args.Length == 0)
{
    Usage("Missing benchmark command. Expected exactly one of:", dispatch);
    return 1;
}

if (dispatch.TryGetValue(args[0], out Action runBenchmark) == false)
{
    Usage($"Unknown benchmark command: '{args[0]}'. Expected exactly one of:", dispatch);
    return 1;
}

// BenchmarkDotNet silently ignores tokens it does not understand (verified
// against 0.14.0: a stray token ran anyway and exited 0), so stray tokens
// must be rejected here or the silent no-op this issue is about comes back.
// A second command token is never valid; a loose token before the first
// --switch cannot be a switch value. After the first --switch, loose tokens
// are treated as switch values (e.g. "--job Dry") and left to BDN.
bool seenSwitch = false;
for (int i = 1; i < args.Length; i++)
{
    if (dispatch.ContainsKey(args[i]) || (seenSwitch == false && IsSwitch(args[i]) == false))
    {
        Console.Error.WriteLine($"Unexpected argument: '{args[i]}'. After the command only BenchmarkDotNet switches (--...) are accepted.");
        return 1;
    }

    if (IsSwitch(args[i]))
    {
        seenSwitch = true;
    }
}

runBenchmark();
return 0;
