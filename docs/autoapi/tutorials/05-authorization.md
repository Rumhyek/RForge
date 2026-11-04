# 05 · Authorization

**Goal:** protect service methods with `[RfAuthorize]` in one place, and get the same rules, and the same exceptions, whether a call goes over HTTP or straight to your service.

## Two paths, one set of rules

| Where the call comes from | HTTP request? | What enforces the rules |
|---|---|---|
| Interactive WebAssembly | Yes | The endpoint: ASP.NET authorization, using the rules from your attributes |
| Interactive Server, static rendering, prerendering | No. The component calls your service directly | A decorator the generator wraps around your registered service |

During an HTTP call the endpoint resolves your service through the same registration, so both checks run. The second one is cheap and keeps the behavior identical.

## 1. Declare the rules

Put the attributes on the interface in the shared library:

```csharp
// MyApp.Shared/IBookService.cs (excerpt)
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

[RForgeAutoApi]
[RfAuthorize]                                          // every method needs a signed-in user...
public interface IBookService
{
    [RfAllowAnonymous]                                 // ...except this one
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BookDto?> GetAsync(int id, CancellationToken cancellationToken = default);    // any signed-in user

    [RfAuthorize(Roles = "Admin,Editor")]              // Admin OR Editor
    Task<BookDto> AddAsync(string title, string author, CancellationToken cancellationToken = default);

    [RfAuthorize(Policy = "CanDelete")]                // the interface rule AND this policy AND the Admin role
    [RfAuthorize(Roles = "Admin")]
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

`[RfAuthorize]` and `[RfAllowAnonymous]` mirror ASP.NET's `AuthorizeAttribute` and `AllowAnonymousAttribute`, so the shared library needs no ASP.NET reference.

| You write | It means |
|---|---|
| `[RfAuthorize]` | A signed-in user |
| `[RfAuthorize(Roles = "Admin,Editor")]` | Signed in, and in `Admin` **or** `Editor` |
| `[RfAuthorize(Policy = "CanDelete")]` (or `[RfAuthorize("CanDelete")]`) | Signed in, and passes the policy `CanDelete` |
| `[RfAuthorize(AuthenticationSchemes = "Bearer")]` | Only identities from those schemes count |
| Several `[RfAuthorize]` (interface plus method, or repeated on a method) | **All** of them must pass |
| `[RfAllowAnonymous]` on a method | No rules for that method, including the ones from the interface |
| `[RfAllowAnonymous]` on the interface | No rules for any method |

With the interface above:

| User | `GetAllAsync` | `GetAsync` | `AddAsync` | `DeleteAsync` |
|---|---|---|---|---|
| Not signed in | Allowed | 401 | 401 | 401 |
| Signed in, no role | Allowed | Allowed | 403 | 403 |
| `Editor` | Allowed | Allowed | Allowed | 403 |
| `Admin` | Allowed | Allowed | Allowed | 403 (also needs `CanDelete`) |
| `Admin` with the `can_delete` claim | Allowed | Allowed | Allowed | Allowed |

## 2. Register authentication, the policy and the endpoints

AutoApi doesn't set up authentication. Use whatever your app uses (cookies, Identity, OpenID Connect, JWT). Only the order below matters:

```csharp
// MyApp/Program.cs (excerpt)
using Microsoft.AspNetCore.Authentication.Cookies;
using MyApp;
using MyApp.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // API calls need a status code, not a redirect to a login page (see step 4).
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("CanDelete", policy => policy.RequireClaim("can_delete")));

builder.Services.AddScoped<IBookService, BookService>();    // 1. your implementation
builder.Services.AddRForgeAutoApiServer();                  // 2. AFTER it: wraps the registration above

var app = builder.Build();

// The template's status code pages must leave the API alone (see step 4).
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRForgeAutoApi();                                     // 3. after UseAuthentication / UseAuthorization
```

## 3. What a failed check looks like

| Situation | Over HTTP | Exception in the component |
|---|---|---|
| Not signed in | `401`, empty body | `RfUnauthorizedException`: "You must be signed in to perform this action." |
| Signed in, not allowed | `403`, empty body | `RfForbiddenException`: "You are not allowed to perform this action." |

In Interactive Server mode the decorator throws the same two exception types with the same messages, so one `catch` works in every render mode:

```razor
@* MyApp.Client/Pages/AddBook.razor *@
@page "/add-book"
@rendermode InteractiveAuto
@using MyApp.Shared
@using RForge.Blazor.AutoApi
@inject IBookService BookService

<button @onclick="AddAsync">Add a book</button>

@if (_message is not null)
{
    <p>@_message</p>
}

@code {
    private string? _message;

    private async Task AddAsync()
    {
        try
        {
            var book = await BookService.AddAsync("Dune Messiah", "Frank Herbert");
            _message = $"Added {book.Title}.";
        }
        catch (RfUnauthorizedException)
        {
            _message = "Sign in to add books.";
        }
        catch (RfForbiddenException ex)
        {
            _message = ex.Message;   // "You are not allowed to perform this action."
        }
    }
}
```

The attributes only know about the *user*. For a rule that depends on the *item* ("only the owner can edit this book"), check it in your service and throw the same exception. The message and `code` you pass reach the component in WebAssembly too:

```csharp
// MyApp/BookService.cs (excerpt)
public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
{
    // ...
    throw new RfForbiddenException("Only the owner can change this book.", "not_owner");
}
```

## 4. Make the API answer with status codes, not pages

Two defaults of a Blazor Web App hide the 401 and 403 from the generated client. Both are fixed in the `Program.cs` above.

| Default | What happens to an unauthenticated or denied call | Fix |
|---|---|---|
| Cookie authentication challenges with a **redirect** | The response is `302` to `/Account/Login?ReturnUrl=…`. `HttpClient` follows it, so the call ends in whatever that URL returns (a `404` if you have no such page), and never reaches `RfUnauthorizedException`. | Answer `401` and `403` for `/api` paths in `OnRedirectToLogin` and `OnRedirectToAccessDenied` |
| The template's `UseStatusCodePagesWithReExecute("/not-found")` | The empty `401`/`403` is re-run as a `POST` to the not-found page, and a `POST` with a JSON body answers `400 The request has an incorrect Content-type.` The client then throws `RfApiException`, not `RfUnauthorizedException`. | Skip the middleware for `/api` with `UseWhen` |

Both checks use `/api`, the start of the default route prefix `/api/auto`. If you change `RoutePrefix` in `[RForgeAutoApiDefaults]`, change them too. Bearer-token schemes (JWT) already answer `401` and `403`, so only the status code pages need the exemption.

## 5. Registration order and lifetimes

`AddRForgeAutoApiServer()` looks up your registration of every `[RForgeAutoApi]` interface and wraps it, keeping its lifetime. It throws at startup if something is wrong:

| You did | Result |
|---|---|
| Registered an interface that has `[RfAuthorize]` as **Singleton** | `InvalidOperationException`: "'MyApp.Shared.IBookService' uses [RfAuthorize] and must be registered as Scoped or Transient, not Singleton, so authorization can see the current user." |
| Didn't register an `[RForgeAutoApi]` interface, or called `AddRForgeAutoApiServer()` before registering it | `InvalidOperationException`: "'MyApp.Shared.IBookService' is not registered. Register your implementation before calling AddRForgeAutoApiServer()." This applies to every interface, with or without rules. |
| Registered the service **again after** `AddRForgeAutoApiServer()` | No error. The new registration replaces the wrapped one, so Interactive Server and prerendered calls are **no longer checked**. The endpoints still enforce the rules over HTTP. |

An interface without any `[RfAuthorize]` can be a Singleton. If a service with rules needs shared state, keep the state in a Singleton store and inject it into the Scoped service, as the **Todos** example (`/autoapi/example/todos`) does.

## 6. How the decorator finds the current user

The decorator needs a user to check. It looks in this order:

| Where the call runs | Source of the user |
|---|---|
| Inside a generated endpoint | `HttpContext.User` of that request |
| A Blazor circuit or a prerender | `AuthenticationStateProvider` |
| Neither is available | `IHttpContextAccessor.HttpContext.User` |
| Still nothing | An anonymous user, so `RfUnauthorizedException` |

`AddRForgeAutoApiServer()` registers `IHttpContextAccessor` for you.

## Best practices

- **Put the rules on the interface, once.** Don't repeat them in endpoint conventions or in the component.
- **Start with `[RfAuthorize]` on the interface** and open single methods with `[RfAllowAnonymous]`. Forgetting a rule then fails closed.
- **Register the service, then `AddRForgeAutoApiServer()`, and register nothing for that interface afterwards.**
- **Catch `RfUnauthorizedException` and `RfForbiddenException` in components**, not `HttpRequestException` or status codes. They are the same in every render mode.
- **Check item-level rules in the service** and throw `RfForbiddenException` or `RfNotFoundException`.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `RfApiException` with status `404`, or a JSON parse error when your login page exists | Cookie challenge redirect followed by `HttpClient` | Answer `401`/`403` for `/api` ([step 4](#4-make-the-api-answer-with-status-codes-not-pages)) |
| `RfApiException` with status `400` instead of `RfUnauthorizedException` | `UseStatusCodePagesWithReExecute` re-ran the `401` | Exempt `/api` with `UseWhen` ([step 4](#4-make-the-api-answer-with-status-codes-not-pages)) |
| Anonymous users can call a protected method from an Interactive Server component | The service was registered after `AddRForgeAutoApiServer()`, or the interface has no `[RfAuthorize]` | Check the registration order ([step 5](#5-registration-order-and-lifetimes)) |
| Startup throws about Scoped or Transient | A service with `[RfAuthorize]` is a Singleton | Register it as Scoped or Transient |
| Always `RfUnauthorizedException` in a circuit, although the user is signed in | `AuthenticationStateProvider` reports an anonymous user | Check how authentication state reaches the circuit; the decorator uses what the provider returns |

**Next:** [06 · Render modes](06-render-modes.md)
