using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RForge.Blazor.Bus.Generators.Tests;

internal sealed record HarnessResult(
    Compilation Output,
    ImmutableArray<Diagnostic> CompilerDiagnostics,
    ImmutableArray<Diagnostic> AnalyzerDiagnostics,
    GeneratorDriverRunResult RunResult)
{
    public IEnumerable<Diagnostic> CompilerErrors =>
        CompilerDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error);

    public string GeneratedSource(string hintSuffix) =>
        RunResult.Results.Single().GeneratedSources.Single(s => s.HintName.EndsWith(hintSuffix, StringComparison.Ordinal)).SourceText.ToString();

    public IEnumerable<string> AnalyzerIds => AnalyzerDiagnostics.Select(static d => d.Id);
}

internal static class Harness
{
    public const string Prelude = """
        using System.Threading;
        using System.Threading.Tasks;
        using RForge.Blazor.Bus;

        namespace App;

        public sealed record Ping(int Value) : IEvent;
        public sealed record Pong(string Text) : IEvent;
        public sealed record Counter(int Count, string Label) : IState<Counter>
        {
            public static Counter Initial => new(0, "");
        }
        """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(static path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToArray<MetadataReference>();

    /// <summary>Compiles <paramref name="sources"/> (path, code) with the generator and analyzer applied.</summary>
    public static HarnessResult Run(params (string Path, string Source)[] sources)
    {
        var trees = new[] { ("Prelude.cs", Prelude) }.Concat(sources)
            .Select(s => CSharpSyntaxTree.ParseText(s.Item2, ParseOptions, s.Item1));

        var compilation = CSharpCompilation.Create(
            "App",
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new BusGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var analyzerDiagnostics = output
            .WithAnalyzers([new BusAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();

        return new HarnessResult(output, output.GetDiagnostics(), analyzerDiagnostics, driver.GetRunResult());
    }

    public static HarnessResult Run(string source) => Run(("Component.cs", source));
}
