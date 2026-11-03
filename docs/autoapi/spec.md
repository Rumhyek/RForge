# RForge Blazor Auto API: Specification

A source generator for Blazor apps that use Interactive Server, Interactive WebAssembly, or Interactive Auto render modes. You write a service interface **once** in a shared library:

- **On the server**, the generator creates minimal API endpoints that call your real implementation. It can also wrap that implementation so `[RfAuthorize]` is enforced in Interactive Server mode too.
- **On the WASM client**, the generator creates an implementation of the same interface that calls those endpoints through `HttpClient`.

Components inject the interface and don't need to know which render mode they're running in. The same calls, exceptions and authorization rules apply everywhere.

---

## 1. Goals

| Goal | How |
| --- | --- |
| One interface for every render mode | Server uses the real implementation; WASM uses a generated HTTP client |
| No hand-written controllers or API clients | Endpoints and clients are generated from the interface |
| The same exceptions in every mode | `RfException` family, sent over the wire as ProblemDetails |
| The same authorization in every mode | `[RfAuthorize]` enforced on the endpoint **and** by a generated server-side decorator |
| Fast and trim-safe in WASM | Uses a `JsonSerializerContext` when you provide one |
| Shared library stays free of ASP.NET | Attributes and exceptions ship in a dependency-free abstractions package |

---

## 2. Packages

| Package | Target | Referenced by | Contents |
| --- | --- | --- | --- |
| `RForge.Blazor.AutoApi.Abstractions` | net8.0; net9.0; net10.0 | Shared library (and, through it, server and client) | Attributes, `RfHttpMethod`, exception types. No ASP.NET dependency. |
| `RForge.Blazor.AutoApi.Generator` | netstandard2.0 (analyzer) | Server project and client project | The incremental source generator and its diagnostics. It depends on `Abstractions` and `Microsoft.Extensions.Http`. Supporting runtime code is emitted as `internal` source, so there is no extra runtime assembly. |

Typical solution layout:

```
MyApp.Shared   -> Abstractions          (interfaces, DTOs, JsonSerializerContext)
MyApp          -> Generator + Shared    (server host, real implementations)
MyApp.Client   -> Generator + Shared    (WASM project)
```

---

## 3. Attributes reference

All attributes live in the `RForge.Blazor.AutoApi` namespace.

### 3.1 `[RForgeAutoApi]` (interface)

Opts in **every method** of the interface. There is no per-method opt-in, because the generated client has to implement the whole interface.

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `HttpClientName` | `string?` | assembly default (`"RForgeAutoApi"`) | The named `HttpClient` used for every method of this interface |
| `RouteSegment` | `string?` | the interface name, e.g. `IOrderService` | The path segment under the route prefix |

### 3.2 `[RForgeAutoApiMethod]` (interface method, optional)

Overrides the defaults for one method.

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `HttpMethod` | `RfHttpMethod` | assembly default (`Post`) | `Get`, `Post`, `Put`, `Patch`, `Delete` |
| `Route` | `string?` | `{RouteSegment}/{MethodName}` | Static and relative to the route prefix. **Placeholders such as `{id}` are not supported** (RFAA007). |
| `HttpClientName` | `string?` | inherited from the interface | |

### 3.3 `[RfAuthorize]` and `[RfAllowAnonymous]` (interface or method)

These mirror `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` and `AllowAnonymousAttribute`, so the shared library doesn't need an ASP.NET reference.

| Property | Type | Notes |
| --- | --- | --- |
| `Policy` | `string?` | Policy name |
| `Roles` | `string?` | Comma-separated, the same as `AuthorizeAttribute` |
| `AuthenticationSchemes` | `string?` | Comma-separated |

- Multiple `[RfAuthorize]` attributes combine. Interface-level and method-level attributes all apply.
- `[RfAllowAnonymous]` on a method overrides any `[RfAuthorize]` on the interface. As in ASP.NET, `[RfAllowAnonymous]` on the interface makes every method anonymous.

### 3.4 `[RForgeAutoApiHost]` (server marker class)

```csharp
[RForgeAutoApiHost(typeof(IOrderService))]   // params Type[]: any type from each target assembly
public static partial class AutoApiEndpoints { }
```

This scans the assemblies that contain the given types for `[RForgeAutoApi]` interfaces and generates `MapRForgeAutoApi()` and `AddRForgeAutoApiServer()` into the class. Use one host class per project (RFAA014).

### 3.5 `[RForgeAutoApiClient]` (client marker class)

```csharp
[RForgeAutoApiClient(typeof(IOrderService))] // params Type[]
public static partial class AutoApiClients { }
```

This generates a client implementation of each `[RForgeAutoApi]` interface, plus `AddRForgeAutoApi()` to register them. Use one client class per project (RFAA014).

---

## 4. Shared-library defaults

Some settings have to be the same on the server and the client. Those are declared **once**, in the shared library, with an assembly-level attribute that both generators read:

```csharp
[assembly: RForgeAutoApiDefaults(
    RoutePrefix       = "/api/auto",               // default
    DefaultHttpMethod = RfHttpMethod.Post,         // default
    HttpClientName    = "RForgeAutoApi",           // default
    JsonContext       = typeof(AppJsonContext),    // default: null (reflection fallback)
    ExcludedAttributes = new[] { typeof(MyDomainOnlyAttribute) })] // added to the deny-list
```

The attribute is optional; when it's missing, the defaults above are used. If a host targets several assemblies, each assembly's endpoints use that assembly's own defaults.

Settings that only matter at runtime, such as handlers, base address, rate limiting and extra endpoint conventions, are configured in code (see sections 5.4 and 6.3).

---

## 5. Server generator

### 5.1 Usage

```csharp
// Program.cs (server)
builder.Services.AddScoped<IOrderService, OrderService>();  // your real implementation
builder.Services.AddRForgeAutoApiServer();                  // authorization decorator (5.6). Call AFTER your registrations.

var app = builder.Build();
// ...
app.MapRForgeAutoApi();                                     // returns RouteGroupBuilder
```

### 5.2 Routes

- Default: `{RoutePrefix}/{RouteSegment}/{MethodName}`, for example `/api/auto/IOrderService/GetOrderAsync`.
- **Overloads:** duplicate default routes get a number added, in declaration order: `GetOrder`, `GetOrder1`, `GetOrder2`. The server and client are generated from the same source, so they always agree. Reordering overloads changes the routes, so deploy the server and client together.
- **Custom routes** are static and relative to the route prefix. A custom route that duplicates another route is an error (RFAA008).
- Methods inherited from base interfaces are included, under the `[RForgeAutoApi]` interface's segment.

### 5.3 What each endpoint does

1. Resolves the interface from `HttpContext.RequestServices`. That's your registration, wrapped by the decorator if you added it.
2. Binds the arguments (section 7).
3. Calls the method, passing `HttpContext.RequestAborted` for any `CancellationToken` parameter.
4. Writes the result: `Task` or `ValueTask` â†’ **204**; `Task<T>` or `ValueTask<T>` â†’ **200** with a JSON body (a `null` result is written as JSON `null`).
5. Turns exceptions into ProblemDetails (section 8).

Illustrative generated code:

```csharp
group.MapPost("IOrderService/GetOrderAsync", static async (HttpContext ctx) =>
{
    var service = ctx.RequestServices.GetRequiredService<global::MyApp.Shared.IOrderService>();
    var args    = await RForgeAutoApiBody.ReadAsync(ctx.Request, ctx.RequestAborted);
    var id      = args.GetRequired("id", global::MyApp.Shared.AppJsonContext.Default.Int32);
    var result  = await service.GetOrderAsync(id, ctx.RequestAborted);
    return Results.Json(result, global::MyApp.Shared.AppJsonContext.Default.OrderDto);
})
.WithName("IOrderService_GetOrderAsync")
.WithTags("IOrderService")
.RequireAuthorization(new global::Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Policy = "Orders.Read" })
.AddEndpointFilter(RForgeAutoApiErrors.Filter);
```

### 5.4 Endpoint conventions and OpenAPI

`MapRForgeAutoApi()` returns a `RouteGroupBuilder` that contains every generated endpoint, with one nested group per route prefix. Conventions you chain onto it apply to all endpoints:

```csharp
app.MapRForgeAutoApi().RequireRateLimiting("api").DisableAntiforgery();
```

Each endpoint gets `.WithTags("{InterfaceName}")` and `.WithName("{InterfaceName}_{MethodName}{n}")`. The **request body schema is not described in OpenAPI in v1**, because the body is a generated envelope rather than a CLR type.

### 5.5 Copying attributes to the endpoint

Every attribute on an interface method, and on the interface itself, is added to the endpoint **in fully qualified form**, except for:

| Excluded | Reason |
| --- | --- |
| RForge attributes (`RForgeAutoApi*`, `RfAuthorize`, `RfAllowAnonymous`) | Translated instead (see below) |
| `System.Runtime.CompilerServices.*` | Compiler-generated (`Nullable`, `AsyncStateMachine`, ...) |
| `System.Diagnostics.CodeAnalysis.*` | Nullability and analysis hints |
| `System.Diagnostics.*` (`DebuggerStepThrough`, `Conditional`, ...) | Debugger and compiler only |
| `System.ObsoleteAttribute` | Applies to callers, not endpoints |
| Anything in `RForgeAutoApiDefaults.ExcludedAttributes` | User-defined |

Translations:
- `[RfAuthorize(...)]` â†’ `.RequireAuthorization(new AuthorizeAttribute { ... })`
- `[RfAllowAnonymous]` â†’ `.AllowAnonymous()`
- Any other attribute â†’ `.WithMetadata(new global::Ns.SomeAttribute(ctorArgs) { Named = ... })`

If an attribute can't be rebuilt from its compile-time arguments, it is skipped and RFAA010 is reported.

### 5.6 Authorization in Interactive Server mode: `AddRForgeAutoApiServer()`

In Interactive Server mode, and during prerendering, components call the **real implementation directly**. No HTTP request is made, so the endpoint's authorization never runs. To give every mode the same protection, the generator emits a **decorator** for each interface:

- `AddRForgeAutoApiServer()` finds your existing registration of each `[RForgeAutoApi]` interface and wraps it, keeping its lifetime. If an interface isn't registered, it throws `InvalidOperationException` at startup.
- Services with authorization rules must be **Scoped or Transient**. A Singleton can't see the current user, so registering one throws at startup.
- Before each call, the decorator checks the method's `[RfAuthorize]`/`[RfAllowAnonymous]` rules with `IAuthorizationService`:
  - The current user comes from `AuthenticationStateProvider` inside a Blazor circuit or prerender, and from `IHttpContextAccessor.HttpContext.User` otherwise.
  - Not authenticated â†’ throws `RfUnauthorizedException`. Authenticated but not authorized â†’ throws `RfForbiddenException`.
- These are the same exceptions the WASM client throws for 401/403, so a component's `catch` blocks behave the same in both modes.
- During an HTTP call, both the endpoint and the decorator check authorization. The second check is cheap and keeps the behavior consistent.

---

## 6. Client generator

### 6.1 Usage

```csharp
// Program.cs (WASM client)
builder.Services.AddRForgeAutoApi();   // returns IHttpClientBuilder for the default "RForgeAutoApi" client
```

### 6.2 Generated client

For each interface, the generator emits a `private sealed class {InterfaceName-without-I}AutoApiClient`, nested inside your marker class, that implements the interface explicitly. `AddRForgeAutoApi()` registers it as **scoped** with `TryAdd`, so if you register your own implementation first, yours wins.

Illustrative generated code:

```csharp
internal sealed class OrderServiceAutoApiClient(IHttpClientFactory factory) : global::MyApp.Shared.IOrderService
{
    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken)
    {
        var http = factory.CreateClient("RForgeAutoApi");
        using var body = RForgeAutoApiBody.Create(w =>
            w.Write("id", id, global::MyApp.Shared.AppJsonContext.Default.Int32));
        using var response = await http.PostAsync("api/auto/IOrderService/GetOrderAsync", body, cancellationToken);
        await RForgeAutoApiErrors.ThrowIfErrorAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync(global::MyApp.Shared.AppJsonContext.Default.OrderDto, cancellationToken);
    }
}
```

Request URLs are relative (no leading `/`), so a base address that includes a path is respected.

### 6.3 HttpClient setup

- `AddRForgeAutoApi()` registers the default named client (`"RForgeAutoApi"`, or whatever `RForgeAutoApiDefaults.HttpClientName` says) **and every other client name your interfaces reference**.
- Each of those clients gets its `BaseAddress` from `IWebAssemblyHostEnvironment.BaseAddress`, unless you've already set one.
- Which client name a method uses: **method > interface > assembly default**.
- To add handlers or change settings, use the returned builder for the default client, or call `AddHttpClient(name)` again for any other name (its configuration is added on top):

```csharp
builder.Services.AddRForgeAutoApi()
    .AddHttpMessageHandler<MyAuthHandler>();

builder.Services.AddHttpClient("ReportsApi", c => c.BaseAddress = new Uri("https://reports.example.com/"));
```

---

## 7. Parameter binding and serialization

### 7.1 Binding

| HTTP method | Parameters | Wire format |
| --- | --- | --- |
| `Post` (default), `Put`, `Patch` | Any supported type | JSON object body: `{ "paramName": value, ... }` |
| `Get`, `Delete` | **Simple types only** (RFAA006) | Query string: `?paramName=value`, formatted culture-invariant; `null` values are left out |

- **Simple types:** primitives, `string`, `decimal`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, enums, and nullable versions of these.
- The body is **always** an object keyed by parameter name (camelCase), even when there's only one parameter. Each value is written separately with `Utf8JsonWriter`, so no DTO types are generated. Adding a parameter that has a default value is therefore a non-breaking change.
- On the server, a missing parameter gets its declared default. If it has no default but is nullable (`string?`, `int?`), it gets `null`. Otherwise the server returns **400** (`RfException`). An unparsable value is also a 400.
- `CancellationToken` parameters are never sent. On the client, the token goes to `HttpClient`; on the server, it's `RequestAborted`. A method can have at most one (RFAA011).

### 7.2 Serialization

WASM is where serialization performance matters. Reflection-based System.Text.Json is slow on the first call and isn't trim-safe. For best performance, declare a `JsonSerializerContext` in the shared library and reference it in `RForgeAutoApiDefaults.JsonContext`:

```csharp
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(int))]
public partial class AppJsonContext : JsonSerializerContext { }
```

- Generated code on both sides uses `JsonTypeInfo<T>` from the context, so there's no reflection.
- If a parameter or return type is missing from the context, **RFAA009** warns at build time, and that type uses the reflection fallback. (Our generator can't add the types for you, because one source generator can't feed its output into another.)
- For the same reason, the context has to live in a **referenced** assembly (the shared library). A context declared in the same project as the marker class looks empty to our generator.
- If no context is configured, generated code falls back to reflection-based STJ with `JsonSerializerDefaults.Web`, and **RFAA012** (Info) is reported.
- In Interactive Server mode, and during prerendering, calls go straight to the implementation, so there's **no serialization at all**.

---

## 8. Error handling

The aim is for the same `catch` block to work in Interactive Server and in WASM.

### 8.1 Exception types (in Abstractions)

| Type | Meaning | Server mode | WASM mode |
| --- | --- | --- | --- |
| `RfException(message, statusCode = 400, code = null)` | An expected error whose **message is safe to show users** | Thrown by your code | Rethrown with the same `Message`, `StatusCode` and `Code` |
| `RfUnauthorizedException : RfException` (401) | Not authenticated | Thrown by the decorator | Thrown on a 401 |
| `RfForbiddenException : RfException` (403) | Not authorized | Thrown by the decorator | Thrown on a 403 |
| `RfNotFoundException : RfException` (404) | The requested item doesn't exist | Thrown by your code | Thrown on a 404 that carries `rfType` (a routing 404 stays `RfApiException`) |
| `RfApiException(statusCode, title, detail, traceId)` | An unexpected server failure (500 and other non-success codes) | n/a: the original exception propagates | Thrown |

Recommended pattern for components:

```csharp
try { order = await Orders.GetOrderAsync(id); }
catch (RfException ex) { message = ex.Message; }          // expected, safe to display (both modes)
catch (Exception ex)   { logger.LogError(ex, "..."); }    // unexpected (RfApiException in WASM, original in Server)
```

### 8.2 On the wire

- `RfException` (and subclasses) â†’ ProblemDetails with `status = StatusCode`, `title = Message`, and extensions `rfType = "RfException"` and `code`. Subclass types other than the two auth exceptions and `RfNotFoundException` **aren't preserved**; the client rethrows a base `RfException`.
- Any other exception â†’ **500** ProblemDetails with a generic title. In the **Development** environment, `detail` also contains the exception type, message and stack trace.
- Authorization failures from ASP.NET (401/403, with or without a body) â†’ `RfUnauthorizedException` / `RfForbiddenException` on the client.
- `OperationCanceledException` is never converted. A canceled request stays a cancellation on both sides.
- Network failures (`HttpRequestException`) on the client are passed through unchanged.

---

## 9. Diagnostics

| ID | Severity | Message |
| --- | --- | --- |
| RFAA001 | Error | `[RForgeAutoApiHost]`/`[RForgeAutoApiClient]` class must be `static partial` |
| RFAA002 | Warning | Target assembly of `{type}` contains no `[RForgeAutoApi]` interfaces |
| RFAA003 | Error | `{method}` returns `{type}`; only `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` are supported |
| RFAA004 | Error | Generic interfaces and generic methods are not supported |
| RFAA005 | Error | `ref`/`out`/`in` parameters are not supported |
| RFAA006 | Error | `{method}` uses `GET`/`DELETE` but parameter `{param}` is not a simple type |
| RFAA007 | Error | Custom route `{route}` contains a placeholder; routes must be static |
| RFAA008 | Error | Route `{route}` is used by more than one method |
| RFAA009 | Warning | `{type}` is not registered in `{JsonContext}` |
| RFAA010 | Warning | Attribute `{attr}` on `{method}` could not be reconstructed and was not copied to the endpoint |
| RFAA011 | Error | `{method}` has more than one `CancellationToken` parameter |
| RFAA012 | Info | No `JsonContext` configured; using reflection-based serialization |
| RFAA013 | Error | `[RForgeAutoApi]` interfaces may contain only instance methods (no properties, events or static abstract members) |
| RFAA014 | Error | Only one `[RForgeAutoApiHost]` / `[RForgeAutoApiClient]` class is allowed per project |

---

## 10. Quick start

**Shared library** (references `RForge.Blazor.AutoApi.Abstractions`)

```csharp
[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext))]

[RForgeAutoApi]
[RfAuthorize]
public interface IOrderService
{
    Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken = default);

    [RfAuthorize(Policy = "Orders.Write")]
    Task SaveOrderAsync(OrderDto order, CancellationToken cancellationToken = default);

    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    [RfAllowAnonymous]
    Task<int> GetOpenOrderCountAsync();
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(int))]
public partial class AppJsonContext : JsonSerializerContext { }
```

**Server** (references the Generator and the Shared library)

```csharp
[RForgeAutoApiHost(typeof(IOrderService))]
public static partial class AutoApiEndpoints { }

// Program.cs
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddRForgeAutoApiServer();
// ...
app.MapRForgeAutoApi();
```

**Client** (references the Generator and the Shared library)

```csharp
[RForgeAutoApiClient(typeof(IOrderService))]
public static partial class AutoApiClients { }

// Program.cs
builder.Services.AddRForgeAutoApi();
```

**Component** (works in any render mode)

```razor
@inject IOrderService Orders

@code {
    OrderDto? order;
    protected override async Task OnInitializedAsync() => order = await Orders.GetOrderAsync(42);
}
```

---

## 11. Repository layout

| Path | Contents |
| --- | --- |
| `src/RForge/RForge.Blazor.AutoApi.Abstractions` | Attributes and exceptions (net8.0, net9.0, net10.0) |
| `src/RForge/RForge.Blazor.AutoApi.Generator` | The incremental generator (netstandard2.0), packed from `RForge.Blazor.AutoApi.Generator.nuspec` |
| `tests/RForge.Blazor.AutoApi.Generator.Tests` | Diagnostics tests, plus a check that generated code compiles |
| `tests/RForge.Blazor.AutoApi.IntegrationTests` (+ `.Shared`) | End-to-end tests: a generated client calling generated endpoints on a `TestServer`, error mapping, authorization over HTTP and in server mode |
| `samples/AutoApiSample` | Blazor Web App (Interactive Auto) with a todo list that switches between Server and WebAssembly |

```bash
dotnet test src/RForge/RForge.sln                           # run all tests
dotnet run --project samples/AutoApiSample/AutoApiSample    # open /todos
dotnet pack src/RForge/RForge.Blazor.AutoApi.Abstractions -c Release -p:Version=0.1.0
dotnet pack src/RForge/RForge.Blazor.AutoApi.Generator -c Release -p:Version=0.1.0
```

To look at the generated code, add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to a consuming project. Files appear under `obj/<config>/<tfm>/generated/RForge.Blazor.AutoApi.Generator/`.
