# 06 · Render modes

**Goal:** know which code answers a call in each render mode, when HTTP is involved, and what to register in which project.

*See it running:* the **Todos** example (`/autoapi/example/todos`).

## The one idea

> **A component injects the interface. Which class it gets depends on where the component runs.**

| Where the component runs | `IBookService` is | HTTP request? | JSON serialization? |
|---|---|---|---|
| Static rendering and prerendering | Your service, resolved on the server | No | No |
| Interactive Server | Your service, resolved in the circuit | No | No |
| Interactive WebAssembly | The generated client | Yes | Yes |
| Interactive Auto | Your service on the server at first, the generated client once it runs in WebAssembly | Only in WebAssembly | Only in WebAssembly |

On the server a call is an ordinary method call. Nothing is serialized, no endpoint runs, and no endpoint convention (rate limiting, filters) applies. [Tutorial 05](05-authorization.md) explains how the rules still apply there.

## 1. Mark and register the server

The server project holds your implementation and the generated endpoints:

```csharp
// MyApp/AutoApiEndpoints.cs
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp;

[RForgeAutoApiHost(typeof(IBookService))]
public static partial class AutoApiEndpoints { }
```

```csharp
// MyApp/Program.cs (excerpt)
builder.Services.AddScoped<IBookService, BookService>();    // your implementation
builder.Services.AddRForgeAutoApiServer();                  // after it

// ...
app.MapRForgeAutoApi();                                     // the endpoints WebAssembly calls
```

## 2. Mark and register the WebAssembly client

The client project holds the generated `HttpClient` implementation:

```csharp
// MyApp.Client/AutoApiClients.cs
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp.Client;

[RForgeAutoApiClient(typeof(IBookService))]
public static partial class AutoApiClients { }
```

```csharp
// MyApp.Client/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRForgeAutoApi();

await builder.Build().RunAsync();
```

The generated requests use relative URLs. In a project that references `Microsoft.AspNetCore.Components.WebAssembly` (every Blazor WebAssembly project does), the client's `BaseAddress` is set to the app's base address for you ([tutorial 07](07-httpclient-setup.md)). Without that reference, set `BaseAddress` yourself.

Which project needs what:

| Project | Package | Needs |
|---|---|---|
| `MyApp.Shared` | `RForge.Blazor.AutoApi.Abstractions` | The interface, its types and the JSON context |
| `MyApp` (server) | `RForge.Blazor.AutoApi.Generator` | The host class, your implementation, `AddRForgeAutoApiServer()`, `MapRForgeAutoApi()` |
| `MyApp.Client` | `RForge.Blazor.AutoApi.Generator` | The client class and `AddRForgeAutoApi()` in its `Program.cs` |

An app that uses only Interactive Server never makes the HTTP call, so it needs no client project. It still uses the host class for `AddRForgeAutoApiServer()`.

## 3. One component, every mode

```razor
@* MyApp.Client/Pages/Books.razor *@
@page "/books"
@rendermode InteractiveAuto
@using MyApp.Shared
@inject IBookService BookService

<h1>Books</h1>
<p>Runtime: @(OperatingSystem.IsBrowser() ? "WebAssembly" : "Server")</p>

@if (_books is null)
{
    <p>Loading...</p>
}
else
{
    <ul>
        @foreach (var book in _books)
        {
            <li>@book.Title by @book.Author</li>
        }
    </ul>
}

@code {
    private List<BookDto>? _books;

    protected override async Task OnInitializedAsync() => _books = await BookService.GetAllAsync();
}
```

The component doesn't mention HTTP, the render mode or the generated client. `OperatingSystem.IsBrowser()` is only there to show where it runs.

When you request `/books`, the server prerenders the page. The prerendered HTML says `Runtime: Server` and already lists the books, and the only HTTP request the server sees is the page itself: the call went straight to `BookService`.

## 4. What Interactive Auto does

1. **Prerender (server).** The component runs once on the server, calling your service directly.
2. **First visit.** The page becomes interactive on the server, over a circuit, while the WebAssembly files download. Calls are still direct.
3. **Later visits.** The page becomes interactive in WebAssembly. Calls are HTTP requests to the generated endpoints.

Blazor starts the component again when it becomes interactive, so `OnInitializedAsync` runs twice: once while prerendering and once when the interactive instance starts. That means two calls to the service. Persist the first result with `PersistentComponentState` if you want to avoid the second.

## 5. What differs between the modes

The interface hides the mode, but a few things still differ:

| | Server (direct call) | WebAssembly (HTTP) |
|---|---|---|
| Returned objects | The very instances your service returned | Copies deserialized from JSON |
| Service lifetime | Scoped = one per circuit, or per request when prerendering | A new scope for **every HTTP request** on the server |
| Endpoint conventions ([tutorial 08](08-endpoint-conventions.md)) | Don't run | Run |
| `CancellationToken` | The token you pass | Goes to `HttpClient`. The service receives the request's `RequestAborted` |
| Expected errors (`RfException` family) | Thrown by your code | Rethrown on the client, with the same type, message, status and `code` |
| Unexpected errors | The original exception | `RfApiException` ([tutorial 04](04-errors.md)) |
| Authorization | The decorator ([tutorial 05](05-authorization.md)) | The endpoint |

Two consequences:

- **Don't keep state in a Scoped service's fields.** A circuit reuses one instance for many calls, but every HTTP request gets a new one, so the same code would behave differently per mode. Keep state in a Singleton store, a database or a cache, and inject it, as the Todos example does with its `DemoTodoStore`.
- **Return immutable types.** If a component mutates the list or object it received, that changes your service's data when the call was direct, and only a copy when it came over HTTP. Records and copies (`list.ToList()`) behave the same in both.

## Best practices

- **Inject the interface everywhere**, never the generated client or the implementation.
- **Register the generated client only in WebAssembly projects**, and the implementation only on the server.
- **Return records and copies**, and keep state outside the service instance.
- **Test in both modes.** Run a page once in Interactive Server and once in WebAssembly, since the two code paths are different.
- **Don't count on ordering between prerender and interactive calls.** Both run, in that order.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `No service for type 'MyApp.Shared.IBookService' has been registered` in the browser | `AddRForgeAutoApi()` missing from `MyApp.Client/Program.cs` | Add it ([step 2](#2-mark-and-register-the-webassembly-client)) |
| The same error on the server | The implementation isn't registered | Register it before `AddRForgeAutoApiServer()` ([tutorial 05](05-authorization.md#5-registration-order-and-lifetimes)) |
| Works on Server, `404` in WebAssembly | `MapRForgeAutoApi()` isn't called, or `RoutePrefix` differs | Map the endpoints, and keep the defaults in one shared library |
| `An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.` | The client project doesn't reference `Microsoft.AspNetCore.Components.WebAssembly`, so no base address was set | Reference it, or set `BaseAddress` ([tutorial 07](07-httpclient-setup.md)) |
| Data differs between Server and WebAssembly | State kept in a Scoped service's fields, or in objects a component mutated | Move state out of the service and return immutable results |
| Items load twice on the first visit | Prerender and interactive start both call the service | Persist the result with `PersistentComponentState` |

**Next:** [07 · HttpClient setup](07-httpclient-setup.md)
