# 08 · Endpoint conventions

**Goal:** apply ASP.NET features such as rate limiting and endpoint filters to every generated endpoint, and know what metadata the endpoints carry.

This tutorial only affects calls over HTTP, from WebAssembly. Calls from Interactive Server components and from prerendering never reach an endpoint, so none of the conventions below run for them ([tutorial 06](06-render-modes.md)).

## 1. Add conventions to the group

`MapRForgeAutoApi()` returns a `RouteGroupBuilder` that contains every generated endpoint. Whatever you chain onto it applies to all of them:

```csharp
// MyApp/Program.cs (excerpt)
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Shared;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

// ...

app.UseRateLimiter();

app.MapRForgeAutoApi()
    .RequireRateLimiting("api")
    .AddEndpointFilter(async (context, next) =>
    {
        var audit = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<AuditAttribute>();
        if (audit is not null) context.HttpContext.Response.Headers["X-Audit"] = audit.Action;

        return await next(context);
    });
```

With `PermitLimit = 3`, five calls to the generated endpoints in a minute answer `200 200 200 429 429`. The limit is counted across the whole group, not per endpoint. Requests to your pages are not affected.

Any other convention works the same way, because the group is an ordinary `RouteGroupBuilder`: `RequireCors`, `RequireHost`, `WithMetadata`, more filters, and so on. Put the middleware they need (`UseRateLimiter`, `UseCors`) in the pipeline before the endpoints run.

## 2. Names, tags and routes

Every endpoint gets a name and a tag. Both are the **interface** name, not the route segment:

| Method | Route | Name | Tag |
|---|---|---|---|
| `IBookService.GetAllAsync` | `/api/auto/IBookService/GetAllAsync` | `IBookService_GetAllAsync` | `IBookService` |
| `IShelfService.CountAsync()` | `/api/auto/shelf/CountAsync` | `IShelfService_CountAsync` | `IShelfService` |
| `IShelfService.CountAsync(string name)` (second overload) | `/api/auto/shelf/CountAsync1` | `IShelfService_CountAsync1` | `IShelfService` |

The second route comes from `RouteSegment = "shelf"` on the interface. Overloads get a number added, in declaration order, to both the route and the name ([tutorial 02](02-methods-and-routes.md)). Tools that read endpoint metadata, such as `EndpointDataSource` or `LinkGenerator`, can use the names and tags.

## 3. Attributes become endpoint metadata

Every attribute you put on an interface or on one of its methods is added to the endpoint as metadata, so your filters and conventions can read it. Define the attribute in the shared library:

```csharp
// MyApp.Shared/AuditAttribute.cs
namespace MyApp.Shared;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
public sealed class AuditAttribute(string action) : Attribute
{
    public string Action { get; } = action;
    public bool Sensitive { get; set; }
}
```

```csharp
// MyApp.Shared/IShelfService.cs
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

[RForgeAutoApi(RouteSegment = "shelf")]
public interface IShelfService
{
    [RfAllowAnonymous]
    [Audit("shelf.count")]
    Task<int> CountAsync();

    [RfAllowAnonymous]
    [Audit("shelf.clear", Sensitive = true)]
    Task ClearAsync();
}
```

The generator writes this into the server's endpoint mapping:

```csharp
// obj/Debug/net10.0/generated/…/MyApp.AutoApiEndpoints.RForgeAutoApi.Server.g.cs (generated; excerpt)
.WithMetadata(new global::MyApp.Shared.AuditAttribute("shelf.clear") { Sensitive = true })
```

and the filter from step 1 sees it. An attribute on the **interface** is copied to every endpoint of that interface, in addition to the method's own.

### What is not copied

| Not copied | Why |
|---|---|
| Every attribute in the `RForge.Blazor.AutoApi` namespace (`RForgeAutoApi*`, `RfAuthorize`, `RfAllowAnonymous`, ...) | Configuration for the generator, or translated instead: `RequireAuthorization(...)` and `AllowAnonymous()` ([tutorial 05](05-authorization.md)) |
| Anything in `System.Runtime.CompilerServices`, `System.Runtime.InteropServices` and `System.Diagnostics` (including `System.Diagnostics.CodeAnalysis`) | Compiler, interop and analysis hints |
| `System.ObsoleteAttribute` and `System.Reflection.DefaultMemberAttribute` | They describe the interface to callers, not the endpoint |
| Attributes on parameters | Only interfaces and methods are read |
| Types listed in `ExcludedAttributes` | Your own deny-list (below) |

Exclude your own attributes in the defaults in the shared library:

```csharp
// MyApp.Shared/IBookService.cs (excerpt)
[assembly: RForgeAutoApiDefaults(
    JsonContext = typeof(AppJsonContext),
    ExcludedAttributes = new[] { typeof(InternalNoteAttribute) })]
```

`[InternalNote("only for the team")]` on a method is then ignored.

### `RFAA010`: an attribute that can't be copied

The generator rebuilds the attribute from its compile-time arguments. Constructor and named arguments of strings, numbers, booleans, enums, `typeof(...)` and arrays all work. If it can't rebuild one, it skips the attribute and warns:

```
warning RFAA010: Attribute 'MyApp.Shared.HiddenAttribute' on 'IShelfService.OddAsync' could not be reconstructed and was not copied to the endpoint
```

The usual cause is an `internal` attribute declared in the shared library, which the server project can't use. Make it `public`, or add it to `ExcludedAttributes` if it shouldn't reach the endpoint anyway (an excluded attribute is skipped before the check, so it gives no warning). The warning is reported by each project that runs the generator, the server and the client.

## 4. What the endpoints don't do

| Feature | What happens |
|---|---|
| **OpenAPI** | With `Microsoft.AspNetCore.OpenApi` (tested with 10.0), the generated endpoints are **not listed** in the document. They carry a name and tags, but their handler is a plain request delegate with no signature to describe, and the request body is a generated envelope rather than a type. |
| **Antiforgery** | Generated endpoints accept JSON, not form posts, so `UseAntiforgery()` doesn't validate them. A `POST` without a token succeeds with `UseAntiforgery()` in the pipeline, and you don't need `DisableAntiforgery()`. |
| **Authorization** | From your `[RfAuthorize]` and `[RfAllowAnonymous]` attributes ([tutorial 05](05-authorization.md)). A convention like `RequireAuthorization()` on the group adds to it. |
| **Interactive Server calls** | None of the conventions above apply ([tutorial 06](06-render-modes.md)). |

If you need rate limiting or auditing for Interactive Server calls too, do it in your service or in a decorator you register around it.

## Best practices

- **Put cross-cutting rules on the group**, once, next to `MapRForgeAutoApi()`.
- **Keep metadata attributes `public`** and in the shared library, and exclude the ones that mean nothing to an endpoint.
- **Read the metadata in one filter or convention** instead of repeating the logic in every service method.
- **Don't rely on conventions for security.** Calls from Interactive Server skip them; use `[RfAuthorize]`, which both paths enforce.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| The rate limit never applies, with no error and nothing in the log | `UseRateLimiter()` is not in the pipeline | Add it before the endpoints run |
| A filter never sees your attribute | The attribute is `internal` (`RFAA010`), excluded, or on a parameter | Make it public, remove it from `ExcludedAttributes`, put it on the method or interface |
| The generated endpoints are missing from your OpenAPI document | The built-in OpenAPI generation can't describe request-delegate endpoints | Document the API another way, or read the endpoint metadata yourself |
| Rate limits don't stop a flood from a Blazor Server app | Interactive Server calls don't go through endpoints | Limit in the service |
| `RFAA010` appears in both the server and the client project | Both run the generator over the same interfaces | Fix the attribute once in the shared library |

**Next:** [09 · Testing](09-testing.md)
