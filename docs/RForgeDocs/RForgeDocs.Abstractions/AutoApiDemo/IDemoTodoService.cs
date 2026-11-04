using System.Text.Json;
using System.Text.Json.Serialization;
using RForge.Blazor.AutoApi;
using RForgeDocs.Abstractions.AutoApiDemo;

[assembly: RForgeAutoApiDefaults(JsonContext = typeof(DemoJsonContext))]

namespace RForgeDocs.Abstractions.AutoApiDemo;

public record DemoTodo(int Id, string Title, bool Done);

/// <summary>The demo list is public, so it is small. The server enforces these.</summary>
public static class DemoLimits
{
    public const int MaxItems = 12;
    public const int MaxTitleLength = 60;
}

/// <summary>
/// One interface for every render mode: the server uses DemoTodoService directly,
/// WebAssembly uses the generated HttpClient implementation.
/// </summary>
[RForgeAutoApi]
public interface IDemoTodoService
{
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<DemoTodo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DemoTodo> AddAsync(string title, CancellationToken cancellationToken = default);

    Task ToggleAsync(int id, CancellationToken cancellationToken = default);

    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete)]
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DemoTodo))]
[JsonSerializable(typeof(List<DemoTodo>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
public partial class DemoJsonContext : JsonSerializerContext;
