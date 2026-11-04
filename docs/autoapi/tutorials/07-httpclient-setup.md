# 07 · HttpClient setup

**Goal:** control how the generated WebAssembly client makes its calls: the base address, authentication tokens, other APIs, and replacing the client altogether.

This tutorial only affects calls made over HTTP, from WebAssembly. In Interactive Server mode and while prerendering there is no `HttpClient` ([tutorial 06](06-render-modes.md)).

## 1. The default client

`AddRForgeAutoApi()` registers one named `HttpClient`, `RForgeAutoApi`, and a generated class that implements each of your interfaces with it:

```csharp
// MyApp.Client/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRForgeAutoApi();

await builder.Build().RunAsync();
```

What you get without any further setup:

| | Behavior |
|---|---|
| Client name | `RForgeAutoApi` (change it with `HttpClientName` in `[RForgeAutoApiDefaults]`) |
| Request URL | Relative, without a leading slash: `api/auto/IBookService/GetAllAsync` |
| Base address | The app's base address (`IWebAssemblyHostEnvironment.BaseAddress`), unless you set one |
| Service lifetime | The generated classes are registered as Scoped |

Because the URLs are relative, a base address that has a path keeps it: with `https://example.com/app/` the call goes to `https://example.com/app/api/auto/IBookService/GetAllAsync`.

The automatic base address needs the client project to reference `Microsoft.AspNetCore.Components.WebAssembly`. Every Blazor WebAssembly project does. In a project without it, no base address is set and you must configure one.

## 2. Add a handler, for example a bearer token

`AddRForgeAutoApi()` returns the `IHttpClientBuilder` of the default client, so you add handlers and settings to it like to any other client:

```csharp
// MyApp.Client/TokenHandler.cs
using System.Net.Http.Headers;

namespace MyApp.Client;

public interface ITokenStore
{
    ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken);
}

/// <summary>Adds the bearer token to every request made by the client it is attached to.</summary>
public sealed class TokenHandler(ITokenStore tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

```csharp
// MyApp.Client/Program.cs (excerpt)
builder.Services.AddSingleton<ITokenStore, SessionTokenStore>();   // your own token source
builder.Services.AddTransient<TokenHandler>();

builder.Services.AddRForgeAutoApi()
    .AddHttpMessageHandler<TokenHandler>();
```

Two things to know:

- **The handler belongs to one named client.** Other clients that AutoApi registers (step 3) don't get it. Add the handler to each one that needs it.
- **Handlers get their own DI scope** from `IHttpClientFactory`, not the one your components use. Register what a handler depends on as a Singleton, as above.

When the API is on the same origin as the app and uses cookie authentication, the browser sends the cookies on its own and you need no handler. For tokens, or an API on another origin, you do.

## 3. Call another API

An interface can use a different named client. Set `HttpClientName` on the interface, and override it on a method if you like:

```csharp
// MyApp.Shared/IReportService.cs
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

[RForgeAutoApi(HttpClientName = "ReportsApi")]
public interface IReportService
{
    Task<decimal> TotalSalesAsync();

    [RForgeAutoApiMethod(HttpClientName = "ExportsApi")]
    Task<string> ExportAsync(string format);
}
```

The name for a method is the first one that is set: **method, then interface, then the assembly default** (`"RForgeAutoApi"`, or `HttpClientName` in `[RForgeAutoApiDefaults]`). With the interfaces in this tutorial:

| Method | Client |
|---|---|
| `IBookService.GetAllAsync` | `RForgeAutoApi` (the default) |
| `IReportService.TotalSalesAsync` | `ReportsApi` (from the interface) |
| `IReportService.ExportAsync` | `ExportsApi` (from the method) |

`AddRForgeAutoApi()` registers **every** client name your interfaces use. In WebAssembly each of them gets the app's base address, so they all call your own server until you say otherwise. To point a client at another server, configure it with `AddHttpClient(name)`:

```csharp
// MyApp.Client/Program.cs (excerpt)
builder.Services.AddRForgeAutoApi()
    .AddHttpMessageHandler<TokenHandler>();

builder.Services.AddHttpClient("ReportsApi", client => client.BaseAddress = new Uri("https://reports.example.com/"))
    .AddHttpMessageHandler<TokenHandler>();
```

Your configuration is added on top of what AutoApi registered, and it wins whether you call `AddHttpClient` before or after `AddRForgeAutoApi()`. The resulting calls:

| Call | URL |
|---|---|
| `IBookService.GetAllAsync` | `https://site.example/app/api/auto/IBookService/GetAllAsync` |
| `IReportService.TotalSalesAsync` | `https://reports.example.com/api/auto/IReportService/TotalSalesAsync` |
| `IReportService.ExportAsync` | `https://site.example/app/api/auto/IReportService/ExportAsync` (no `ExportsApi` base address was set) |

The route is the same everywhere. The other server must run the generated endpoints for `IReportService` (its own host class and `MapRForgeAutoApi()`), use the same `RoutePrefix`, and allow your origin with CORS when it is a different origin.

To change the base address of the default client itself, use the builder `AddRForgeAutoApi()` returns:

```csharp
// MyApp.Client/Program.cs (excerpt)
builder.Services.AddRForgeAutoApi()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://api.example.com/v2/"));
// IBookService.GetAllAsync now calls https://api.example.com/v2/api/auto/IBookService/GetAllAsync
```

## 4. Replace the generated client

The generated classes are registered with `TryAddScoped`, so an implementation you registered **before** `AddRForgeAutoApi()` is kept. One you register after it replaces the generated one, as any later registration does. Either way yours wins:

```csharp
// MyApp.Client/Program.cs (excerpt)
builder.Services.AddScoped<IBookService, CachingBookService>();   // your own, perhaps wrapping HttpClient calls
builder.Services.AddRForgeAutoApi();
```

This is the place for a cache, an offline store or a test double. The server isn't affected; it has its own registration.

## ✅ Best practices

- **Add handlers to the builder `AddRForgeAutoApi()` returns**, and to each extra named client that needs them.
- **Register a handler's dependencies as Singletons.**
- **Give each external API its own interface** and `HttpClientName`, so one `AddHttpClient` call configures all its methods.
- **Keep `RoutePrefix` and the client names in `[RForgeAutoApiDefaults]`** in the shared library, so the server and the client always read the same values.
- **Leave the app's base address alone** when the API is the app's own server.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Calls to `IReportService` have no `Authorization` header | The handler was added to the default client only | Add the handler to `ReportsApi` as well |
| `IReportService` calls your own server instead of the reports server | No `BaseAddress` configured for `ReportsApi`, so it kept the app's | `AddHttpClient("ReportsApi", c => c.BaseAddress = …)` |
| `An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.` | The client project doesn't reference `Microsoft.AspNetCore.Components.WebAssembly` | Reference it, or set `BaseAddress` |
| Calls to a path-based host go to the wrong URL | `BaseAddress` has no trailing slash, so its last segment is dropped | End it with `/`: `https://example.com/app/` |
| A handler can't read a Scoped service your components share | `IHttpClientFactory` gives handlers a separate DI scope | Use a Singleton |
| `404` from the other server | Its generated endpoints aren't mapped, or its `RoutePrefix` differs | Map the same interfaces and use the same defaults |

**Next:** [08 · Endpoint conventions](08-endpoint-conventions.md)
