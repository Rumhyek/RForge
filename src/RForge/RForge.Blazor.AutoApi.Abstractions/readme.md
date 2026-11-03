# RForge Blazor Auto API

**Write a service interface once and call it from any Blazor render mode.**

With Blazor's Interactive Auto render mode, the same component can run on the server (Interactive Server) or in the browser (WebAssembly). On the server it can call your services directly. In the browser it has to go through HTTP. Usually that means writing everything twice: an API endpoint for each method, plus an `HttpClient` implementation of the interface for the WebAssembly project.

RForge Auto API is a source generator that writes both halves for you at compile time:

- **Server project:** minimal API endpoints that call your real implementation.
- **WebAssembly project:** an implementation of your interface that calls those endpoints.

Your components just `@inject IMyService`. Calls, exceptions and authorization behave the same whichever mode the component ends up in.

```
                       ┌──────────────────────── Shared library ────────────────────────┐
                       │  [RForgeAutoApi] public interface IOrderService { ... }        │
                       └────────────────────────────────────────────────────────────────┘
                                   │                                    │
                ┌──────── Server project ───────┐          ┌──── WebAssembly project ────┐
 Interactive    │  OrderService (your code)     │   HTTP   │  generated                  │
 Server ───────▶│  ▲                            │◀─────────│  OrderServiceAutoApiClient  │◀── WebAssembly
 component      │  └─ generated endpoints       │          │  : IOrderService            │    component
                │     /api/auto/IOrderService/* │          └─────────────────────────────┘
                └───────────────────────────────┘
```

No reflection at runtime, no hand-written controllers, no hand-written API clients.

---

## Contents

- [Getting started](#getting-started)
- [How calls map to HTTP](#how-calls-map-to-http)
- [Error handling](#error-handling)
- [Authorization](#authorization)
- [Serialization and performance](#serialization-and-performance)
- [Configuration](#configuration)
- [Rules and limitations](#rules-and-limitations)
- [Troubleshooting](#troubleshooting)
- [Working on this repository](#working-on-this-repository)

For the complete reference (every attribute property, generated code, all diagnostics), see the **[specification](https://github.com/Rumhyek/RForge/blob/main/docs/autoapi/spec.md)**.

---

## Getting started

### 1. Install the packages

| Project | Package |
| --- | --- |
| Shared library (holds the interfaces) | `RForge.Blazor.AutoApi.Abstractions` |
| Server project | `RForge.Blazor.AutoApi.Generator` |
| WebAssembly (`.Client`) project | `RForge.Blazor.AutoApi.Generator` |

```bash
dotnet add MyApp.Shared package RForge.Blazor.AutoApi.Abstractions
dotnet add MyApp        package RForge.Blazor.AutoApi.Generator
dotnet add MyApp.Client package RForge.Blazor.AutoApi.Generator
```

The abstractions package supports .NET 8, 9 and 10. The generator package brings in the abstractions and `Microsoft.Extensions.Http`.

### 2. Describe the service in the shared library

```csharp
using RForge.Blazor.AutoApi;

[RForgeAutoApi]                       // every method of this interface becomes an endpoint
public interface ITodoService
{
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItem> AddAsync(string title, CancellationToken cancellationToken = default);

    [RfAuthorize(Roles = "Admin")]
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public record TodoItem(int Id, string Title, bool Done);
```

### 3. Server project: implement, register and map

```csharp
// AutoApi.cs: a marker class; the generator adds extension methods to it
[RForgeAutoApiHost(typeof(ITodoService))]   // any type from the shared library
public static partial class AutoApiEndpoints { }
```

```csharp
// Program.cs
builder.Services.AddScoped<ITodoService, TodoService>();   // your real implementation
builder.Services.AddRForgeAutoApiServer();                 // after your registrations

var app = builder.Build();
// ...
app.MapRForgeAutoApi();                                    // the endpoints WebAssembly calls
```

### 4. WebAssembly project: register the generated client

```csharp
// AutoApi.cs
[RForgeAutoApiClient(typeof(ITodoService))]
public static partial class AutoApiClients { }
```

```csharp
// Program.cs
builder.Services.AddRForgeAutoApi();
```

### 5. Use it from any component

```razor
@page "/todos"
@rendermode InteractiveAuto
@inject ITodoService Todos

@code {
    private List<TodoItem>? items;

    protected override async Task OnInitializedAsync() => items = await Todos.GetAllAsync();
}
```

When the page runs on the server, `Todos` is your `TodoService`. When it runs in WebAssembly, `Todos` is the generated client calling `/api/auto/ITodoService/GetAllAsync`. The component code is identical in both cases.

A working version of this example is in [samples/AutoApiSample](https://github.com/Rumhyek/RForge/tree/main/samples/AutoApiSample).

---

## How calls map to HTTP

| Interface | HTTP |
| --- | --- |
| Route | `/api/auto/{InterfaceName}/{MethodName}`, e.g. `/api/auto/ITodoService/AddAsync` |
| Overloads | Numbered in declaration order: `EchoAsync`, `EchoAsync1`, `EchoAsync2` |
| Default HTTP method | `POST` |
| `POST` / `PUT` / `PATCH` parameters | JSON object body keyed by parameter name: `{ "title": "Milk" }` |
| `GET` / `DELETE` parameters | Query string (`?id=5`). Only simple types: numbers, `string`, `bool`, `Guid`, dates and times, enums, and their nullable forms |
| `Task` / `ValueTask` | `204 No Content` |
| `Task<T>` / `ValueTask<T>` | `200` with a JSON body |
| `CancellationToken` | Never sent. The client passes it to `HttpClient`; on the server it's the request-aborted token |

You can override the method or route per method:

```csharp
[RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete, Route = "todos/remove")]
Task DeleteAsync(int id);
```

Custom routes are static: placeholders such as `{id}` aren't supported. Parameters always travel in the body or the query string.

If a parameter is missing from a request, the server uses its declared default. If it has no default but is nullable, it gets `null`. Otherwise the server answers 400. This means adding an optional parameter doesn't break older clients.

---

## Error handling

The goal is that the same `catch` block works in every render mode.

```csharp
try
{
    await Todos.AddAsync(newTitle);
}
catch (RfException ex)
{
    // Expected error. ex.Message is safe to show to the user.
    error = ex.Message;
}
```

Throw `RfException` from your service for errors the user should see:

```csharp
if (string.IsNullOrWhiteSpace(title))
    throw new RfException("A title is required.", statusCode: 400, code: "title_required");
```

| What happens | Interactive Server | WebAssembly |
| --- | --- | --- |
| Your code throws `RfException` | The same `RfException` | `RfException` with the same `Message`, `StatusCode` and `Code` |
| User isn't signed in | `RfUnauthorizedException` | `RfUnauthorizedException` |
| User lacks permission | `RfForbiddenException` | `RfForbiddenException` |
| Your code throws `RfNotFoundException` | The same `RfNotFoundException` | `RfNotFoundException` with the same `Message` and `Code` |
| Any other exception | The original exception | `RfApiException` (status 500, generic message) |
| Cancellation | `OperationCanceledException` | `OperationCanceledException` |

`RfUnauthorizedException`, `RfForbiddenException` and `RfNotFoundException` all derive from `RfException`.

On the wire, errors are [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) responses. Unexpected exceptions are logged on the server. Their details, including the stack trace, are only sent to the client in the Development environment.

---

## Authorization

`[RfAuthorize]` and `[RfAllowAnonymous]` mirror ASP.NET's `[Authorize]` and `[AllowAnonymous]`. They exist so your shared library doesn't need an ASP.NET reference. You can put them on the interface, on individual methods, or both.

```csharp
[RForgeAutoApi]
[RfAuthorize]                                  // every method requires a signed-in user...
public interface IOrderService
{
    Task<OrderDto?> GetAsync(int id);

    [RfAuthorize(Policy = "Orders.Write")]     // ...this one also requires a policy
    Task SaveAsync(OrderDto order);

    [RfAllowAnonymous]                         // ...and this one is public
    Task<int> CountAsync();
}
```

These rules are enforced in **both** paths:

- **WebAssembly → HTTP:** the generated endpoints call `RequireAuthorization(...)` / `AllowAnonymous()`, so your normal authentication middleware applies.
- **Interactive Server → direct call:** there's no HTTP request, so `AddRForgeAutoApiServer()` wraps your service in a generated decorator. It checks the same rules against the current user before each call. The user comes from `AuthenticationStateProvider` inside a circuit, or from `HttpContext` otherwise.

Requirements:
- Call `builder.Services.AddAuthorization()`.
- Register services that use `[RfAuthorize]` as **Scoped** or **Transient**, so they can see the current user.

---

## Serialization and performance

In WebAssembly, reflection-based `System.Text.Json` is slow on the first call and isn't safe to trim. For the best performance, declare a `JsonSerializerContext` in the shared library and point the generator at it:

```csharp
[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext))]

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(TodoItem))]
[JsonSerializable(typeof(List<TodoItem>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
public partial class AppJsonContext : JsonSerializerContext;
```

- Generated code then uses the context's compiled serializers directly, on both the server and the client.
- If a parameter or return type is missing from the context, you get build warning **RFAA009**, and that type falls back to reflection.
- Without a context, everything uses reflection with web defaults (camelCase), and you get an informational **RFAA012**.
- Interactive Server calls don't serialize anything; they're plain method calls.

The context must be in the shared library, not in the server or client project. Source generators can't see each other's output, so a context in the same project looks empty to this generator.

---

## Configuration

### Shared defaults (in the shared library)

Settings that both sides must agree on live in one assembly attribute:

```csharp
[assembly: RForgeAutoApiDefaults(
    RoutePrefix        = "/api/auto",                // default
    DefaultHttpMethod  = RfHttpMethod.Post,          // default
    HttpClientName     = "RForgeAutoApi",            // default
    JsonContext        = typeof(AppJsonContext),
    ExcludedAttributes = new[] { typeof(MyDomainAttribute) })]
```

### Per interface and per method

```csharp
[RForgeAutoApi(RouteSegment = "orders", HttpClientName = "OrdersApi")]
public interface IOrderService { ... }
```

When a client name is set in more than one place: **method > interface > assembly default**.

### Server endpoints

`MapRForgeAutoApi()` returns a `RouteGroupBuilder` containing every generated endpoint. Anything you chain onto it applies to all of them:

```csharp
app.MapRForgeAutoApi()
   .RequireRateLimiting("api")
   .RequireCors("frontend");
```

Each endpoint is tagged with its interface name and named `{Interface}_{Method}`, so it shows up sensibly in OpenAPI.

Other attributes on your interface methods (your own endpoint metadata, for example) are copied onto the endpoints. Compiler, nullability and debugger attributes, and anything listed in `ExcludedAttributes`, are skipped.

### WebAssembly HttpClient

`AddRForgeAutoApi()` registers a named `HttpClient` for every client name your interfaces use. Each one's base address defaults to the app's own host. The default client's builder is returned, so you can add handlers or override the base address:

```csharp
builder.Services.AddRForgeAutoApi()
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient("OrdersApi", c => c.BaseAddress = new Uri("https://orders.example.com/"));
```

The generated clients are registered as scoped, using `TryAdd`. If you register your own implementation first, yours wins.

---

## Rules and limitations

The generator reports a build error or warning when an interface breaks one of these rules. The full table is in the [specification](https://github.com/Rumhyek/RForge/blob/main/docs/autoapi/spec.md#9-diagnostics).

- Every method must return `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`.
- No generic interfaces or generic methods. No `ref` / `out` / `in` parameters. At most one `CancellationToken` per method.
- Interfaces may contain only instance methods: no properties or events.
- `GET` / `DELETE` methods may only take simple parameter types.
- Routes must be unique and static.
- Use one `[RForgeAutoApiHost]` and one `[RForgeAutoApiClient]` class per project.

Things to know:

- **Only the base `RfException` crosses the wire.** Custom subclasses arrive in WebAssembly as plain `RfException` with the same message, status and code. The exceptions are `RfUnauthorizedException`, `RfForbiddenException` and `RfNotFoundException`.
- **Overload routes depend on declaration order.** Reordering overloads changes their routes, so deploy the server and client together. (They're always rebuilt together in a normal Blazor Web App.)
- **OpenAPI doesn't describe request bodies yet.** The body is a generated envelope rather than a C# type.

---

## Troubleshooting

**I want to see the generated code.** Add this to the server or client project:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
```

The files appear under `obj/<Configuration>/<TargetFramework>/generated/RForge.Blazor.AutoApi.Generator/`.

**`'IMyService' is not registered. Register your implementation before calling AddRForgeAutoApiServer().`** Move `AddRForgeAutoApiServer()` below your service registrations. Every `[RForgeAutoApi]` interface needs a server implementation.

**`... must be registered as Scoped or Transient`.** A service with `[RfAuthorize]` rules is registered as a Singleton. Change its lifetime.

**WebAssembly calls go to the wrong host.** Configure the client's base address on the builder returned by `AddRForgeAutoApi()`, as shown in [WebAssembly HttpClient](#webassembly-httpclient).

**`MapRForgeAutoApi` / `AddRForgeAutoApi` doesn't exist.** Check that the marker class is `public static partial`, isn't nested, and that the generator package is referenced by that project. Look for build error RFAA001.

---

## Working on this repository

| Path | Contents |
| --- | --- |
| [src/RForge/RForge.Blazor.AutoApi.Abstractions](https://github.com/Rumhyek/RForge/tree/main/src/RForge/RForge.Blazor.AutoApi.Abstractions) | Attributes and exceptions |
| [src/RForge/RForge.Blazor.AutoApi.Generator](https://github.com/Rumhyek/RForge/tree/main/src/RForge/RForge.Blazor.AutoApi.Generator) | The incremental source generator |
| [tests/RForge.Blazor.AutoApi.Generator.Tests](https://github.com/Rumhyek/RForge/tree/main/tests/RForge.Blazor.AutoApi.Generator.Tests) | Diagnostics tests and a "generated code compiles" check |
| [tests/RForge.Blazor.AutoApi.IntegrationTests](https://github.com/Rumhyek/RForge/tree/main/tests/RForge.Blazor.AutoApi.IntegrationTests) | End-to-end tests against an in-memory server |
| [samples/AutoApiSample](https://github.com/Rumhyek/RForge/tree/main/samples/AutoApiSample) | Interactive Auto Blazor Web App (`/todos`) |
| [docs/autoapi/spec.md](https://github.com/Rumhyek/RForge/blob/main/docs/autoapi/spec.md) | Full specification |

```bash
dotnet build src/RForge/RForge.sln
dotnet test  src/RForge/RForge.sln
dotnet run --project samples/AutoApiSample/AutoApiSample     # then open /todos

dotnet pack src/RForge/RForge.Blazor.AutoApi.Abstractions -c Release -p:Version=0.1.0 -o artifacts
dotnet pack src/RForge/RForge.Blazor.AutoApi.Generator    -c Release -p:Version=0.1.0 -o artifacts
```

Requires the .NET 10 SDK.
