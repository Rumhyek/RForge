using AutoApiSample.Shared;
using RForge.Blazor.AutoApi;

namespace AutoApiSample;

[RForgeAutoApiHost(typeof(ITodoService))]
public static partial class AutoApiEndpoints { }

/// <summary>In-memory storage shared by every request and circuit.</summary>
public sealed class TodoStore
{
    private readonly Lock _lock = new();
    private readonly List<TodoItem> _items = [new(1, "Try Interactive Server", false), new(2, "Try WebAssembly", false)];
    private int _nextId = 3;

    public T Use<T>(Func<List<TodoItem>, Func<int>, T> action)
    {
        lock (_lock) return action(_items, () => _nextId++);
    }
}

/// <summary>The real implementation. Interactive Server calls it directly; WebAssembly reaches it through the generated endpoints.</summary>
public sealed class TodoService(TodoStore store) : ITodoService
{
    public Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(store.Use((items, _) => items.ToList()));

    public Task<TodoItem> AddAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new RfException("A title is required.", code: "title_required");

        return Task.FromResult(store.Use((items, nextId) =>
        {
            var item = new TodoItem(nextId(), title.Trim(), false);
            items.Add(item);
            return item;
        }));
    }

    public Task ToggleAsync(int id, CancellationToken cancellationToken = default)
    {
        store.Use((items, _) =>
        {
            var index = items.FindIndex(i => i.Id == id);
            if (index < 0) throw new RfException($"Todo {id} was not found.", 404);
            items[index] = items[index] with { Done = !items[index].Done };
            return true;
        });
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        store.Use((items, _) => items.RemoveAll(i => i.Id == id));
        return Task.CompletedTask;
    }
}
