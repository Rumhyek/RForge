# 03 · Parameters and serialization

**Goal:** know how method parameters and return values travel over HTTP, what happens when one is missing or invalid, and how to keep JSON fast and trim-safe in WebAssembly.

This tutorial assumes the projects from [tutorial 01](01-setup.md). You only edit the shared library.

## 1. How parameters are sent

How a method's parameters travel depends on its HTTP method:

| HTTP method | Parameters | Wire format |
|---|---|---|
| `Post` (default), `Put`, `Patch` | Any type the JSON context knows | A JSON object keyed by parameter name: `{ "filter": {...}, "page": 2 }` |
| `Get`, `Delete` | **Simple types only** | The query string: `?genre=Science&page=2` |

Here is an interface that uses both:

```csharp
// MyApp.Shared/BookDto.cs
namespace MyApp.Shared;

public enum Genre { Fiction, Science, History }

public record BookDto(int Id, string Title, string Author, Genre Genre, DateOnly? PublishedOn);

public record BookFilter(string? TitleContains, Genre? Genre);
```

```csharp
// MyApp.Shared/IBookService.cs
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

[RForgeAutoApi]
public interface IBookService
{
    // GET: simple types only, sent in the query string
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<BookDto>> FindAsync(
        string? titleContains,
        Genre genre = Genre.Fiction,
        int page = 1,
        DateOnly? since = null,
        CancellationToken cancellationToken = default);

    // POST: any type, sent as a JSON object keyed by parameter name
    Task<List<BookDto>> SearchAsync(BookFilter filter, int page = 1, CancellationToken cancellationToken = default);

    Task<BookDto> AddAsync(BookDto book, string? note = null);

    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete)]
    Task DeleteAsync(Guid id, Genre genre);
}
```

The requests look like this:

```bash
# FindAsync: a query string. The client leaves out null values.
curl "https://localhost:5001/api/auto/IBookService/FindAsync?titleContains=du&genre=Fiction&page=2&since=1960-01-01"

# SearchAsync: a body keyed by parameter name
curl -X POST https://localhost:5001/api/auto/IBookService/SearchAsync \
     -H "Content-Type: application/json" \
     -d '{"filter":{"titleContains":"x","genre":2},"page":3}'
```

The body is **always** an object keyed by parameter name, even for a single parameter. Each value is written separately, so the generator never creates a request type, and adding a parameter with a default value later doesn't break existing callers.

The generated client sends every parameter on every call, including the ones that have default values.

## 2. Simple types

`Get` and `Delete` accept only simple types, because there is no good way to put an object in a query string. The simple types are:

- numbers (`int`, `long`, `decimal`, `double` and the other numeric types), `bool` and `char`
- `string` and `Guid`
- `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` and `TimeSpan`
- enums
- the nullable versions of all of these

Values are formatted with the invariant culture, so a query string reads the same on every machine. Dates use the round-trip format (`1960-01-01`), and an enum is sent by name (`genre=Science`). The server accepts either the name or the number (`genre=1`).

Any other parameter type on a `Get` or `Delete` method fails the build with `RFAA006`:

```csharp
// MyApp.Shared/IBadBookService.cs (does not build)
[RForgeAutoApi]
public interface IBadBookService
{
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<BookDto>> FindAsync(BookFilter filter);   // error RFAA006

    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete)]
    Task DeleteAsync(List<int> ids);                    // error RFAA006
}
```

```
error RFAA006: 'IBadBookService.FindAsync' uses GET but parameter 'filter' of type 'MyApp.Shared.BookFilter' is not a simple type
```

Fix it by passing the pieces separately, or by using `Post`.

`ref`, `out` and `in` parameters are not supported (`RFAA005`).

## 3. Missing, optional and invalid values

The server decides what to do with each parameter:

| Request | The server |
|---|---|
| The parameter is present and valid | Passes it to your method |
| It is missing and has a **default value** | Uses the default (`page = 1`, `genre = Genre.Fiction`) |
| It is missing, has no default, and is **nullable** (`string?`, `int?`, `DateOnly?`) | Passes `null` |
| It is missing and is neither | Answers `400` with `Missing required parameter 'filter'.` |
| It can't be read as the parameter's type (`page=abc`, `genre=Poetry`) | Answers `400` with `Invalid value for parameter 'page'.` |
| The body isn't valid JSON | Answers `400` with `The request body is not valid JSON.` |

For example, `FindAsync` called with no query string at all runs with `titleContains = null`, `genre = Fiction`, `page = 1` and `since = null`. A call to `DeleteAsync` without `genre` fails:

```json
{"title":"Missing required parameter 'genre'.","status":400,"traceId":"00-7ef4...","rfType":"RfException"}
```

Other details:

- **Unknown properties are ignored.** A body with an extra `"extra": 1` still works.
- **A JSON `null` for a non-nullable parameter reaches your method as `null`.** The server does not enforce nullable annotations, so validate input that matters inside the service.
- **Adding a parameter with a default value is a compatible change.** An older client that doesn't send it gets the default. Adding a parameter *without* a default breaks older clients, because the server answers `400`.

The `400` answers are `RfException` errors, so a component sees them as `RfException` too ([tutorial 04](04-errors.md)).

## 4. CancellationToken

A `CancellationToken` parameter is never sent over the wire:

- on the **server**, it receives `HttpContext.RequestAborted`, so your method stops when the browser abandons the request;
- on the **client**, the token you pass goes to `HttpClient`, which cancels the request.

A method can have at most one (`RFAA011`):

```
error RFAA011: 'IBadService.RunAsync' has more than one CancellationToken parameter
```

Put it last and give it `= default`, as in the examples, so callers can omit it.

## 5. The JSON context

WebAssembly is where serialization cost matters. Reflection-based `System.Text.Json` is slow on the first call and isn't trim-safe. A `JsonSerializerContext` removes the reflection, and the generated code on both sides uses it. Declare one in the shared library and name it in the assembly attribute, as in [tutorial 01](01-setup.md):

```csharp
// MyApp.Shared/AppJsonContext.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using MyApp.Shared;
using RForge.Blazor.AutoApi;

[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext))]

namespace MyApp.Shared;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(BookDto))]
[JsonSerializable(typeof(List<BookDto>))]
[JsonSerializable(typeof(BookFilter))]
public partial class AppJsonContext : JsonSerializerContext;
```

Every type the generated code reads or writes has to be known to the context. These are the parameter types of `Post`, `Put` and `Patch` methods and the type inside every `Task<T>`. Query-string values (`Get`, `Delete`) don't go through JSON. A type counts as known when you register it, or when a type you registered uses it: `BookDto` has an `int`, a `string`, a `Genre` and a `DateOnly?`, so those need no entry of their own. For a type that is neither, the build warns:

```
warning RFAA009: 'MyApp.Shared.BookFilter' is not registered in 'global::MyApp.Shared.AppJsonContext'; reflection-based serialization will be used for it
```

Add a `[JsonSerializable]` line for it. If you ignore the warning the call still works, but that type uses reflection and isn't trim-safe. Simple types such as `Guid`, `decimal` and `bool` are not exempt: `Task<Guid> CreateAsync()` warns until you add `[JsonSerializable(typeof(Guid))]`.

**The context has to live in a referenced assembly.** The generator reads the context from the assembly's compiled metadata, because one source generator can't use the output of another. A context declared in the same project as the marker class looks empty, so every type in it reports `RFAA009`. Keep the context next to the interfaces in the shared library.

### Enums as strings

`JsonSerializerDefaults.Web` writes enums as numbers (`"genre": 2`). To send names instead, add one option to the context. The server still accepts numbers:

```csharp
// MyApp.Shared/AppJsonContext.cs
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
```

### Without a context

If you don't set `JsonContext`, the generated code falls back to reflection with the web defaults, and the build reports `RFAA012`. It is an informational message, so `dotnet build` doesn't print it; it shows in the IDE's error list. Everything still works. You only lose the trim-safe, reflection-free path.

## 6. Where serialization happens

| Render mode | What the call does |
|---|---|
| Static rendering and prerendering | Calls your service directly. No HTTP, no JSON. |
| Interactive Server | Calls your service directly. No HTTP, no JSON. |
| WebAssembly | The generated client serializes the arguments, sends the request, and reads the JSON response. |

Because Server mode never serializes, a type that can't survive JSON (a cycle, a type with no suitable converter) works on the server and fails in WebAssembly. Put the type in the context and test the WebAssembly path ([tutorial 09](09-testing.md)).

## How it works

- For `Post`, `Put` and `Patch`, the generated client writes each argument with `Utf8JsonWriter` using the type information from your context, and the server reads each value out of the body by name.
- For `Get` and `Delete`, the client builds the query string itself and the server converts each value to the parameter type.
- The context is read by both generators, so the server and the client always serialize the same way.

## Best practices

- **Register every type you send or return** in the JSON context, and treat `RFAA009` as something to fix, not to ignore.
- **Use records for data types.** They serialize without ceremony and compare by value.
- **Give new parameters a default value** so older clients keep working during a rolling deployment.
- **Keep `Get` for simple reads.** Use `Post` as soon as a parameter is an object.
- **Validate input in the service.** Nullable annotations aren't enforced on the wire.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `RFAA006` on a `Get` or `Delete` method | A parameter isn't a simple type | Pass simple values, or use `Post` |
| `RFAA009` for `Guid`, `decimal`, `List<int>` | The type isn't known to the context | Add `[JsonSerializable(typeof(...))]` |
| `RFAA009` for every type | The context is declared in the same project as the marker class | Move it to the shared library |
| `400 Missing required parameter` after a release | A parameter was added without a default value | Give it a default, or deploy server and client together |
| `400 Invalid value for parameter` for an enum | The value is neither a valid name nor a number | Send `Science` or `1`, not `Poetry` |
| A call works on the server but fails in WebAssembly | The type can't round-trip through JSON | Add it to the context and test in WebAssembly |
| `RFAA011` | Two `CancellationToken` parameters | Keep one |

**Next:** [04 · Errors](04-errors.md)
