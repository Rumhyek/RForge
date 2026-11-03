using System.Text.Json;
using System.Text.Json.Serialization;
using AutoApiSample.Shared;
using RForge.Blazor.AutoApi;

[assembly: RForgeAutoApiDefaults(JsonContext = typeof(SampleJsonContext))]

namespace AutoApiSample.Shared;

public record TodoItem(int Id, string Title, bool Done);

/// <summary>
/// One interface for every render mode: the server uses TodoService directly,
/// WebAssembly uses the generated HttpClient implementation.
/// </summary>
[RForgeAutoApi]
public interface ITodoService
{
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItem> AddAsync(string title, CancellationToken cancellationToken = default);

    Task ToggleAsync(int id, CancellationToken cancellationToken = default);

    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete)]
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(TodoItem))]
[JsonSerializable(typeof(List<TodoItem>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
public partial class SampleJsonContext : JsonSerializerContext;
