# 01 · Setup

**Goal:** add RForge.Blazor.AutoApi to a Blazor Web App, describe a service once, and call it from a component that works in every render mode.

*See it running:* the **Todos** example (`/autoapi/example/todos`).

This tutorial uses a Blazor Web App with **Auto** interactivity. It has a server project (`MyApp`) and a client project (`MyApp.Client`), and we add a shared library (`MyApp.Shared`) for the interface. If your app has no WebAssembly project, you don't need AutoApi: components that only run on the server can call your services directly.

## 1. Create the projects

```bash
dotnet new blazor -int Auto -n MyApp
cd MyApp
dotnet new classlib -n MyApp.Shared
dotnet sln add MyApp.Shared
dotnet add MyApp.Client reference MyApp.Shared
dotnet add MyApp reference MyApp.Shared
```

`MyApp.Shared` holds the interface and the types it uses. Both the server and the client reference it, because the server needs the interface to implement it and the client needs it to generate a copy.

## 2. Lay out the project (recommended, not required)

This is the layout the tutorials use. It is a recommendation: you can organize files however suits your app. Only these four things are required:

1. The interface lives in a project that both the server and the client reference.
2. The server and the client projects each reference `RForge.Blazor.AutoApi.Generator` (NuGet doesn't pass analyzers through project references, so [step 3](#3-reference-the-packages) adds it to both).
3. There is one `[RForgeAutoApiHost]` class in the server project and one `[RForgeAutoApiClient]` class in the client project (`RFAA014` reports a second one). Both must be `static partial`.
4. The `JsonSerializerContext` you name in `[assembly: RForgeAutoApiDefaults]` lives in a **referenced** assembly, not in the project that has the marker class ([tutorial 03](03-parameters-and-serialization.md)).

The recommended layout:

```
MyApp.Shared/                          interfaces and the types they use (no ASP.NET reference)
├── BookDto.cs                         data types sent over the wire
├── IBookService.cs                    [RForgeAutoApi] interface
└── AppJsonContext.cs                  JsonSerializerContext + [assembly: RForgeAutoApiDefaults]
MyApp/                                 server project (Blazor Web App host)
├── Program.cs                         registers the service, adds and maps the generated endpoints
├── AutoApiEndpoints.cs                [RForgeAutoApiHost] marker class
└── BookService.cs                     your real implementation
MyApp.Client/                          WebAssembly project
├── Program.cs                         registers the generated HttpClient implementation
├── AutoApiClients.cs                  [RForgeAutoApiClient] marker class
└── Pages/                             components that inject IBookService
```

Why we suggest these locations:

| File | Location | Why |
|---|---|---|
| Interface and data types | `MyApp.Shared/` | One definition for both sides. Keeping ASP.NET out of this project means any project can reference it. |
| Real implementation | `MyApp/` | It uses databases, secrets and server APIs, so it must not ship to the browser. |
| Marker classes | Next to each `Program.cs` | The generator adds the extension methods you call from `Program.cs` to these classes. |
| Components | `MyApp.Client/` | Components that run in WebAssembly have to live in a project the browser downloads. The server can still render them. |

## 3. Reference the packages

| Project | Package |
|---|---|
| `MyApp.Shared` | `RForge.Blazor.AutoApi.Abstractions` |
| `MyApp` | `RForge.Blazor.AutoApi.Generator` |
| `MyApp.Client` | `RForge.Blazor.AutoApi.Generator` |

```bash
dotnet add MyApp.Shared package RForge.Blazor.AutoApi.Abstractions
dotnet add MyApp        package RForge.Blazor.AutoApi.Generator
dotnet add MyApp.Client package RForge.Blazor.AutoApi.Generator
```

or in each `.csproj`:

```xml
<!-- MyApp.Shared/MyApp.Shared.csproj -->
<ItemGroup>
  <PackageReference Include="RForge.Blazor.AutoApi.Abstractions" Version="1.0.0" />
</ItemGroup>
```

```xml
<!-- MyApp/MyApp.csproj and MyApp.Client/MyApp.Client.csproj -->
<ItemGroup>
  <PackageReference Include="RForge.Blazor.AutoApi.Generator" Version="1.0.0" />
</ItemGroup>
```

The abstractions package holds the attributes and exception types and has no ASP.NET dependency. The generator package is an analyzer: it adds no runtime assembly, and it brings in the abstractions and `Microsoft.Extensions.Http`.

> **Tip:** the `RForge.Blazor` package installs the whole family, including both AutoApi packages. Reference it in the server and client projects where you would reference the generator, and keep the shared library on the abstractions package so it stays small.

## 4. Describe the service in the shared library

```csharp
// MyApp.Shared/BookDto.cs
namespace MyApp.Shared;

public record BookDto(int Id, string Title, string Author);
```

```csharp
// MyApp.Shared/IBookService.cs
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

[RForgeAutoApi]
public interface IBookService
{
    Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default);
}
```

`[RForgeAutoApi]` opts in **every method** of the interface. There is no per-method opt-in, because the generated client has to implement the whole interface.

Now the JSON context. It lists every type that crosses the wire so serialization needs no reflection, and the `[assembly: RForgeAutoApiDefaults]` line tells both generators where to find it:

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
[JsonSerializable(typeof(int))]
public partial class AppJsonContext : JsonSerializerContext;
```

Every parameter type and return type needs a `[JsonSerializable]` entry. [Tutorial 03](03-parameters-and-serialization.md) explains what happens when one is missing.

## 5. Implement, register and map on the server

```csharp
// MyApp/BookService.cs
using MyApp.Shared;

namespace MyApp;

public sealed class BookService : IBookService
{
    private static readonly BookDto[] Books =
    [
        new(1, "Dune", "Frank Herbert"),
        new(2, "Hyperion", "Dan Simmons"),
    ];

    public Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Books.FirstOrDefault(b => b.Id == id));
}
```

```csharp
// MyApp/AutoApiEndpoints.cs
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp;

[RForgeAutoApiHost(typeof(IBookService))]   // any type from the shared library
public static partial class AutoApiEndpoints { }
```

The type you pass only tells the generator which **assembly** to scan. It finds every `[RForgeAutoApi]` interface in that assembly, so one type is enough however many interfaces the library has.

In `Program.cs`, register your service, then add the generated wrapper, then map the endpoints:

```csharp
// MyApp/Program.cs (changes only)
using MyApp;
using MyApp.Shared;

builder.Services.AddScoped<IBookService, BookService>();   // your real implementation
builder.Services.AddRForgeAutoApiServer();                 // after your registrations

var app = builder.Build();
// ...
app.MapStaticAssets();
app.MapRForgeAutoApi();                                    // the endpoints WebAssembly calls
```

The generator adds `MapRForgeAutoApi()` and `AddRForgeAutoApiServer()` to your marker class, in the marker class's namespace. That is why `Program.cs` needs `using MyApp;`.

`AddRForgeAutoApiServer()` wraps your existing registration so authorization is enforced in Interactive Server mode too ([tutorial 05](05-authorization.md)). It finds your registration, so call it **after** `AddScoped`. If the interface isn't registered yet, the app fails at startup:

```
System.InvalidOperationException: 'MyApp.Shared.IBookService' is not registered. Register your implementation before calling AddRForgeAutoApiServer().
```

## 6. Register the generated client

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

// Registers the generated HttpClient implementation of IBookService.
builder.Services.AddRForgeAutoApi();

await builder.Build().RunAsync();
```

`AddRForgeAutoApi()` registers the implementation as Scoped. It uses `TryAdd`, so an implementation you registered earlier wins. The server project does **not** call it: on the server, `IBookService` is your `BookService`.

## 7. Use it from a component

```razor
@* MyApp.Client/Pages/Book.razor *@
@page "/book"
@rendermode InteractiveAuto
@using MyApp.Shared
@inject IBookService Books

<PageTitle>Book</PageTitle>

@if (book is null)
{
    <p>Loading...</p>
}
else
{
    <p>@book.Title by @book.Author</p>
}

@code {
    private BookDto? book;

    protected override async Task OnInitializedAsync() => book = await Books.GetBookAsync(1);
}
```

The component only knows `IBookService`. During prerendering and in Interactive Server mode, `Books` is your `BookService`. After the page switches to WebAssembly, `Books` is the generated client, which posts to `/api/auto/IBookService/GetBookAsync`. Run the app and open `/book`: both give `Dune by Frank Herbert`.

You can call the endpoint yourself. The body is a JSON object keyed by parameter name:

```bash
curl -X POST https://localhost:5001/api/auto/IBookService/GetBookAsync \
     -H "Content-Type: application/json" -d '{"id":1}'
# {"id":1,"title":"Dune","author":"Frank Herbert"}
```

A method that returns `null` answers `200` with the JSON body `null`.

## 8. Look at the generated code

Add `EmitCompilerGeneratedFiles` to a project to write the generated files to disk:

```xml
<!-- MyApp/MyApp.csproj and MyApp.Client/MyApp.Client.csproj -->
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

After a build they appear under `obj/<configuration>/<framework>/generated/RForge.Blazor.AutoApi.Generator/RForge.Blazor.AutoApi.Generator.AutoApiGenerator/`:

| Project | File | Contains |
|---|---|---|
| `MyApp` | `MyApp.AutoApiEndpoints.RForgeAutoApi.Server.g.cs` | `MapRForgeAutoApi()`, `AddRForgeAutoApiServer()` and one handler per method |
| `MyApp.Client` | `MyApp.Client.AutoApiClients.RForgeAutoApi.Client.g.cs` | `AddRForgeAutoApi()` and a private `BookServiceAutoApiClient` that implements `IBookService` |

The other `.g.cs` files are small runtime helpers the generator emits as `internal` source, so there is no extra assembly to deploy.

## How it works

- The generator reads `[RForgeAutoApi]` interfaces from the assemblies named in the marker attributes. It writes the server half into the `[RForgeAutoApiHost]` class and the client half into the `[RForgeAutoApiClient]` class.
- Each method becomes `POST /api/auto/{InterfaceName}/{MethodName}` by default. [Tutorial 02](02-methods-and-routes.md) shows how to change that.
- The server and the client are generated from the same interface, so their routes and parameter names always agree.
- Nothing uses reflection at runtime when you provide a `JsonSerializerContext`.

## Best practices

- **Keep the shared library free of ASP.NET.** It only needs the abstractions package, so any project can reference it.
- **Name the JSON context in `[assembly: RForgeAutoApiDefaults]` from the start**, so the generated code never falls back to reflection.
- **Register your implementation, then `AddRForgeAutoApiServer()`.** Keep the two lines together in `Program.cs`.
- **Deploy the server and the client together.** The routes come from the same source, and a stale client calls routes that may have changed.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `CS1061: 'IServiceCollection' does not contain a definition for 'AddRForgeAutoApi'` | The client project has no `RForge.Blazor.AutoApi.Generator` reference, or `Program.cs` is missing `using MyApp.Client;` | Add the package to `MyApp.Client`, and import the namespace of the marker class |
| `'MyApp.Shared.IBookService' is not registered` at startup | `AddRForgeAutoApiServer()` ran before your `AddScoped`, or an `[RForgeAutoApi]` interface in the scanned assembly has no registration at all | Register an implementation of every `[RForgeAutoApi]` interface first |
| The page works on the server but fails after it switches to WebAssembly | The client's `Program.cs` doesn't call `AddRForgeAutoApi()`, or `MapRForgeAutoApi()` is missing on the server | Add the missing call |
| `RFAA014` error | A second `[RForgeAutoApiHost]` or `[RForgeAutoApiClient]` class in the same project | Keep one marker class per project |
| `RFAA009` warning | A parameter or return type is missing from the JSON context | See [tutorial 03](03-parameters-and-serialization.md) |

**Next:** [02 · Methods and routes](02-methods-and-routes.md)
