# 02 · Methods and routes

**Goal:** know which URL and HTTP method every interface method gets, and change them when the defaults don't fit.

*See it running:* the **Todos** example (`/autoapi/example/todos`) uses one `Get`, one `Delete` and two default `Post` methods.

This tutorial assumes the three projects from [tutorial 01](01-setup.md). You only edit `IBookService` and the defaults in the shared library. The server and the client are generated from the same source, so they always agree on every route in this tutorial.

## 1. The default route

```csharp
// MyApp.Shared/IBookService.cs
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

[RForgeAutoApi]
public interface IBookService
{
    Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default);

    Task<string> DescribeAsync(int id);
}
```

Every method becomes one endpoint:

```
POST /api/auto/{InterfaceName}/{MethodName}
```

| Method | Endpoint |
|---|---|
| `GetBookAsync` | `POST /api/auto/IBookService/GetBookAsync` |
| `DescribeAsync` | `POST /api/auto/IBookService/DescribeAsync` |

The method name is used as written, including the `Async` suffix.

## 2. Choose the HTTP method

`[RForgeAutoApiMethod]` overrides the defaults for one method:

```csharp
// MyApp.Shared/IBookService.cs
[RForgeAutoApi]
public interface IBookService
{
    // GET /api/auto/IBookService/GetAllAsync
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default);

    // PUT /api/auto/IBookService/AddAsync
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Put)]
    Task<BookDto> AddAsync(string title, string author);

    // POST /api/auto/IBookService/GetBookAsync (the default)
    Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default);
}
```

| `RfHttpMethod` | Parameters are sent as |
|---|---|
| `Post` (default), `Put`, `Patch` | A JSON object in the request body |
| `Get`, `Delete` | The query string, and only [simple types](03-parameters-and-serialization.md#1-how-parameters-are-sent) are allowed |

The generated endpoint only answers its own HTTP method.

To change the default for the whole library, set `DefaultHttpMethod` in the assembly attribute. Methods without their own `HttpMethod` use it:

```csharp
// MyApp.Shared/AppJsonContext.cs
[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext), DefaultHttpMethod = RfHttpMethod.Put)]
```

## 3. Change the route prefix and segment

The route is `{RoutePrefix}/{RouteSegment}/{MethodName}`. The prefix defaults to `/api/auto` and the segment to the interface name. Set the prefix once for the library, and the segment per interface:

```csharp
// MyApp.Shared/AppJsonContext.cs
[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext), RoutePrefix = "/api/v1")]
```

```csharp
// MyApp.Shared/IBookService.cs
[RForgeAutoApi(RouteSegment = "books")]
public interface IBookService
{
    // PUT /api/v1/books/GetBookAsync   (with DefaultHttpMethod = Put from section 2)
    Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default);
}
```

Without `RouteSegment`, the same method would be `/api/v1/IBookService/GetBookAsync`.

## 4. Use a custom route

`Route` replaces the `{RouteSegment}/{MethodName}` part. It is relative to the prefix:

```csharp
// MyApp.Shared/IBookService.cs
// DELETE /api/v1/books/remove?id=3
[RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete, Route = "books/remove")]
Task DeleteAsync(int id);
```

Custom routes are **static**. A placeholder such as `books/{id}` is an error (`RFAA007`), because parameters travel in the body or the query string, never in the path. Two methods with the same route are also an error (`RFAA008`).

## 5. Overloads

Overloads would map to the same route, so the generator adds a number to the later ones, in declaration order:

```csharp
// MyApp.Shared/IBookService.cs
Task<string> DescribeAsync(int id);                    // POST /api/v1/IBookService/DescribeAsync
Task<string> DescribeAsync(int id, bool verbose);      // POST /api/v1/IBookService/DescribeAsync1
```

The client numbers them the same way, so calls reach the right method. The numbers follow declaration order, so **reordering overloads changes their routes**. Deploy the server and the client together, or give each overload its own `Route`, which doesn't change when you reorder.

## 6. Inherited methods

Methods from base interfaces are included, under the segment of the `[RForgeAutoApi]` interface you declared. The base interface doesn't need the attribute:

```csharp
// MyApp.Shared/IBookService.cs
public interface IBookReader
{
    Task<int> CountAsync();
}

[RForgeAutoApi]
public interface IBookService : IBookReader
{
    // ...
}
```

`CountAsync` is served at `POST /api/v1/IBookService/CountAsync`.

## 7. Return types and status codes

| Return type | Response |
|---|---|
| `Task`, `ValueTask` | `204 No Content` |
| `Task<T>`, `ValueTask<T>` | `200 OK` with the JSON value |
| `Task<T>` that returns `null` | `200 OK` with the JSON body `null` |

Nothing else is allowed. `void` and plain `string` fail the build (`RFAA003`), because the client has to be asynchronous:

```csharp
// MyApp.Shared/IBadService.cs (does not build)
[RForgeAutoApi]
public interface IBadService
{
    void Save();          // error RFAA003: 'IBadService.Save' returns 'void'; only Task, Task<T>, ValueTask and ValueTask<T> are supported
    string Name();        // error RFAA003
}
```

## 8. What the generator rejects

All of these are build errors, reported on the marker class (`[RForgeAutoApiHost]` and `[RForgeAutoApiClient]`):

| Code | Rule | Example |
|---|---|---|
| `RFAA003` | The return type must be `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>` | `void Save();` |
| `RFAA004` | No generic interfaces and no generic methods | `Task<T> GetAsync<T>(int id);` |
| `RFAA007` | Custom routes can't contain a placeholder | `Route = "books/{id}"` |
| `RFAA008` | Two methods can't share a route | Two methods with `Route = "shared"` |
| `RFAA013` | Interfaces may contain only instance methods | `string Name { get; }` |

For example, a duplicate route reports:

```
error RFAA008: Route '/api/v1/shared' is used by more than one method ('IBadDuplicate.AAsync' and 'IBadDuplicate.BAsync')
```

## How it works

- The route is built once from the interface, the method and the assembly defaults. The server maps it and the client calls it.
- Each endpoint accepts exactly one HTTP method. A request with another method doesn't reach your service.
- The prefix, the default HTTP method and the JSON context are read from `[assembly: RForgeAutoApiDefaults]`.

## ✅ Best practices

- **Use `Get` for reads that take simple parameters**, so browsers and proxies can treat them as reads. Keep the default `Post` for everything that takes objects.
- **Version the prefix** (`/api/v1`) in the assembly attribute if other clients will call these endpoints.
- **Give overloads distinct names**, or a `Route` each, when you expect to reorder or remove them.
- **Keep the method names stable.** The method name is part of the route.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `RFAA003` on a synchronous method | The return type isn't a `Task` or `ValueTask` | Return `Task` or `Task<T>` |
| `RFAA007` on a route | `{id}` placeholder in `Route` | Send `id` as a parameter instead |
| `RFAA013` on a property | The interface declares a property or event | Replace it with a method |
| An old client gets the wrong overload after a release | Overloads were reordered, which renumbers their routes | Deploy server and client together, or give each overload its own `Route` |
| A custom `Route` starts with `/api/...` and the URL is doubled | `Route` is relative to the prefix | Remove the prefix from the route |

**Next:** [03 · Parameters and serialization](03-parameters-and-serialization.md)
