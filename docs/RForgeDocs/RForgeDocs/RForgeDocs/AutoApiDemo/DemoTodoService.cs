using RForge.Blazor.AutoApi;
using RForgeDocs.Abstractions.AutoApiDemo;

namespace RForgeDocs.AutoApiDemo;

[RForgeAutoApiHost(typeof(IDemoTodoService))]
public static partial class AutoApiEndpoints { }

/// <summary>
/// In-memory storage shared by every visitor. The site is public, so the list is small and
/// goes back to its starting items every <see cref="ResetAfter"/>.
/// </summary>
public sealed class DemoTodoStore
{
    public static readonly TimeSpan ResetAfter = TimeSpan.FromMinutes(30);

    private readonly object _lock = new();
    private List<DemoTodo> _items = [];
    private int _nextId;
    private DateTime _resetAt = DateTime.MinValue;

    public T Use<T>(Func<List<DemoTodo>, Func<int>, T> action)
    {
        lock (_lock)
        {
            if (DateTime.UtcNow >= _resetAt) Reset();
            return action(_items, () => _nextId++);
        }
    }

    private void Reset()
    {
        _items = [new(1, "Try Interactive Server", false), new(2, "Try WebAssembly", false)];
        _nextId = 3;
        _resetAt = DateTime.UtcNow + ResetAfter;
    }
}

/// <summary>The real implementation. Interactive Server calls it directly; WebAssembly reaches it through the generated endpoints.</summary>
public sealed class DemoTodoService(DemoTodoStore store) : IDemoTodoService
{
    public Task<List<DemoTodo>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(store.Use((items, _) => items.ToList()));

    public Task<DemoTodo> AddAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new RfException("A title is required.", code: "title_required");
        title = title.Trim();
        if (title.Length > DemoLimits.MaxTitleLength)
            throw new RfException($"A title can have at most {DemoLimits.MaxTitleLength} characters.", code: "title_too_long");

        return Task.FromResult(store.Use((items, nextId) =>
        {
            if (items.Count >= DemoLimits.MaxItems)
                throw new RfException($"The demo list is full ({DemoLimits.MaxItems} items). Delete one first.", 409, "list_full");

            var item = new DemoTodo(nextId(), title, false);
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
