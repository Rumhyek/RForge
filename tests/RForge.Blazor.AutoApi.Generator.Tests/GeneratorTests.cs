using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RForge.Blazor.AutoApi.Generator.Tests;

public sealed class GeneratorTests
{
    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
        .ToArray();

    private const string Usings = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using RForge.Blazor.AutoApi;
        """;

    private static (ImmutableArray<Diagnostic> Generator, ImmutableArray<Diagnostic> CompileErrors) Run(string source)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(Usings + "\n" + source, new CSharpParseOptions(LanguageVersion.CSharp12)) },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create(new AutoApiGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        return (diagnostics, errors);
    }

    private static string Ids(IEnumerable<Diagnostic> diagnostics) => string.Join(", ", diagnostics.Select(d => d.Id).Distinct().OrderBy(id => id));

    private static void AssertReports(string id, string source)
    {
        var (diagnostics, _) = Run(source);
        Assert.Contains(diagnostics, d => d.Id == id);
    }

    private const string Markers = """
        [RForgeAutoApiHost(typeof(IApi))] public static partial class Endpoints { }
        """;

    [Fact]
    public void Valid_host_and_client_generate_code_that_compiles()
    {
        var (diagnostics, errors) = Run("""
            public record Item(int Id, string Name);
            public enum Kind { A, B }

            [RForgeAutoApi]
            [RfAuthorize(Policy = "p")]
            public interface IApi : IBaseApi
            {
                Task<Item?> GetAsync(int id, CancellationToken cancellationToken = default);
                Task SaveAsync(Item item, string? note = null);
                [RfAllowAnonymous, RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
                ValueTask<int> CountAsync(Kind kind = Kind.B, Guid? owner = null, DateTimeOffset? since = null, decimal min = 1.5m);
                ValueTask DeleteAsync(@string @class);
                Task<List<Item>> GetAsync(string @event);
            }

            public interface IBaseApi
            {
                Task<string> PingAsync();
            }

            public sealed class @string { }

            [RForgeAutoApiHost(typeof(IApi))] public static partial class Endpoints { }
            [RForgeAutoApiClient(typeof(IApi))] public static partial class Clients { }
            """);

        Assert.True(errors.IsEmpty, string.Join(Environment.NewLine, errors));
        Assert.Equal("RFAA012", Ids(diagnostics));
    }

    [Fact]
    public void Marker_must_be_static_partial() => AssertReports("RFAA001", """
        [RForgeAutoApi] public interface IApi { Task PingAsync(); }
        [RForgeAutoApiHost(typeof(IApi))] public partial class Endpoints { }
        """);

    [Fact]
    public void Assembly_without_interfaces_warns() => AssertReports("RFAA002", """
        [RForgeAutoApiClient(typeof(object))] public static partial class Clients { }
        """);

    [Fact]
    public void Unsupported_return_type_is_an_error() => AssertReports("RFAA003", """
        [RForgeAutoApi] public interface IApi { int Get(); }
        """ + Markers);

    [Fact]
    public void Generic_method_is_an_error() => AssertReports("RFAA004", """
        [RForgeAutoApi] public interface IApi { Task<T> GetAsync<T>(); }
        """ + Markers);

    [Fact]
    public void Generic_interface_is_an_error() => AssertReports("RFAA004", """
        [RForgeAutoApi] public interface IApi<T> { Task<T> GetAsync(); }
        [RForgeAutoApiHost(typeof(IApi<>))] public static partial class Endpoints { }
        """);

    [Fact]
    public void Ref_parameter_is_an_error() => AssertReports("RFAA005", """
        [RForgeAutoApi] public interface IApi { Task SetAsync(in int value); }
        """ + Markers);

    [Fact]
    public void Get_with_complex_parameter_is_an_error() => AssertReports("RFAA006", """
        public record Filter(string Name);
        [RForgeAutoApi] public interface IApi { [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)] Task<int> CountAsync(Filter filter); }
        """ + Markers);

    [Fact]
    public void Route_placeholder_is_an_error() => AssertReports("RFAA007", """
        [RForgeAutoApi] public interface IApi { [RForgeAutoApiMethod(Route = "items/{id}")] Task<int> GetAsync(int id); }
        """ + Markers);

    [Fact]
    public void Duplicate_custom_route_is_an_error() => AssertReports("RFAA008", """
        [RForgeAutoApi] public interface IApi
        {
            [RForgeAutoApiMethod(Route = "same")] Task OneAsync();
            [RForgeAutoApiMethod(Route = "same")] Task TwoAsync();
        }
        """ + Markers);

    [Fact]
    public void Type_missing_from_json_context_warns() => AssertReports("RFAA009", """
        [assembly: RForgeAutoApiDefaults(JsonContext = typeof(EmptyContext))]
        public abstract class EmptyContext { }
        public record Item(int Id);
        [RForgeAutoApi] public interface IApi { Task<Item> GetAsync(); }
        """ + Markers);

    [Fact]
    public void Multiple_cancellation_tokens_are_an_error() => AssertReports("RFAA011", """
        [RForgeAutoApi] public interface IApi { Task RunAsync(CancellationToken a, CancellationToken b); }
        """ + Markers);

    [Fact]
    public void Properties_are_an_error() => AssertReports("RFAA013", """
        [RForgeAutoApi] public interface IApi { int Count { get; } }
        """ + Markers);

    [Fact]
    public void Second_marker_class_is_an_error() => AssertReports("RFAA014", """
        [RForgeAutoApi] public interface IApi { Task PingAsync(); }
        [RForgeAutoApiHost(typeof(IApi))] public static partial class Endpoints { }
        [RForgeAutoApiHost(typeof(IApi))] public static partial class MoreEndpoints { }
        """);

    [Fact]
    public void Overloads_get_numbered_routes_and_custom_route_collisions_are_detected()
    {
        var (diagnostics, _) = Run("""
            [RForgeAutoApi] public interface IApi
            {
                Task<int> GetAsync(int id);
                Task<int> GetAsync(string name);
                [RForgeAutoApiMethod(Route = "IApi/GetAsync1")] Task OtherAsync();
            }
            """ + Markers);

        var duplicate = Assert.Single(diagnostics, d => d.Id == "RFAA008");
        Assert.Contains("/api/auto/IApi/GetAsync1", duplicate.GetMessage());
    }
}
