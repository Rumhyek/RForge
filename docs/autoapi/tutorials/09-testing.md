# 09 · Testing

**Goal:** test the code AutoApi generates, and the components that use it. You will run the real generated client against the real generated endpoints in memory, check that errors and authorization behave the same in every render mode, and test components with a fake service and no HTTP at all.

The examples use the `IBookService` interface below. Everything here works for any interface.

```csharp
// MyApp.Shared/IBookService.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using MyApp.Shared;
using RForge.Blazor.AutoApi;

[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext))]

namespace MyApp.Shared;

public record BookDto(int Id, string Title, string Author);

[RForgeAutoApi]
[RfAuthorize]
public interface IBookService
{
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    [RfAllowAnonymous]
    Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default);

    [RfAuthorize(Roles = "Librarian")]
    Task<BookDto> AddBookAsync(BookDto book, CancellationToken cancellationToken = default);
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(BookDto))]
[JsonSerializable(typeof(List<BookDto>))]
[JsonSerializable(typeof(int))]
public partial class AppJsonContext : JsonSerializerContext { }
```

## Two levels of test

| Level | What it proves | What it needs |
|---|---|---|
| **Round trip** (sections 2 to 5) | The generated client and the generated endpoints agree: arguments, results, error types, status codes and authorization. | An in-memory server (`TestServer`) and both generators. |
| **Component** (section 6) | A component shows the right thing for what the service returns or throws. | A fake `IBookService`. No server, no HTTP. |

Test your real service logic the usual way, as plain unit tests of the class. This tutorial is about the parts AutoApi adds around it.

## 1. Create the test project

```xml
<!-- MyApp.Tests/MyApp.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="bunit" Version="2.11.3" />
    <PackageReference Include="Microsoft.AspNetCore.TestHost" Version="10.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="RForge.Blazor.AutoApi.Generator" Version="1.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApp.Shared\MyApp.Shared.csproj" />
    <ProjectReference Include="..\MyApp.Client\MyApp.Client.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

Why the test project references the generator itself: the generated code lives in the project that has the marker class, and NuGet doesn't pass analyzers through project references. The test project declares its own marker classes (next section), so it needs its own generator reference. The shared library comes in through the project reference, along with the abstractions package. Drop `bunit` and `MyApp.Client` if you only do round-trip tests.

Use your app's target framework in place of `net10.0` if it differs. The tests only need a framework the AutoApi packages support (.NET 8, 9 or 10).

## 2. Describe the server under test

```csharp
// MyApp.Tests/TestApi.cs
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp.Tests;

// The test project runs both generators: one marker class builds the endpoints, the other the client.
[RForgeAutoApiHost(typeof(IBookService))]
public static partial class TestEndpoints { }

[RForgeAutoApiClient(typeof(IBookService))]
public static partial class TestClients { }

/// <summary>Holds the books for one test host, and lets a test make the service fail.</summary>
public sealed class BookStore
{
    public List<BookDto> Books { get; } = [new(1, "Dune", "Frank Herbert")];
    public Exception? FailWith { get; set; }
}

/// <summary>A stand-in for the real service. It follows the same rules, without a database.</summary>
public sealed class InMemoryBookService(BookStore store) : IBookService
{
    public Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(store.Books.ToList());

    public Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default)
    {
        if (store.FailWith is not null) throw store.FailWith;

        var book = store.Books.FirstOrDefault(b => b.Id == id)
            ?? throw new RfNotFoundException($"Book {id} was not found.", "book_not_found");
        return Task.FromResult<BookDto?>(book);
    }

    public Task<BookDto> AddBookAsync(BookDto book, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(book.Title)) throw new RfException("A title is required.", 422, "title_required");

        var added = book with { Id = store.Books.Max(b => b.Id) + 1 };
        store.Books.Add(added);
        return Task.FromResult(added);
    }
}
```

- The two marker classes generate `MapRForgeAutoApi()` and `AddRForgeAutoApiServer()` (host), and `AddRForgeAutoApi()` (client), as extension methods in `MyApp.Tests`. Each project can have one marker class of each kind ([troubleshooting](10-troubleshooting.md), `RFAA014`).
- `InMemoryBookService` can be your real service with a fake repository behind it, or a small stand-in like this one. What matters is that it throws the same `Rf…` exceptions as the real one.
- `BookStore.FailWith` lets a test simulate a bug (an unexpected exception) without a special service class.

Next, a way to sign a request in without a real identity provider:

```csharp
// MyApp.Tests/TestAuthHandler.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyApp.Tests;

/// <summary>Signs a request in from the X-Test-User and X-Test-Roles headers.</summary>
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.Name, user.ToString()) };
        if (Request.Headers.TryGetValue("X-Test-Roles", out var roles))
            claims.AddRange(roles.ToString().Split(',').Select(r => new Claim(ClaimTypes.Role, r)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
```

A request with no `X-Test-User` header is anonymous. The client factory in the next file adds the headers.

## 3. Start the server and make clients

```csharp
// MyApp.Tests/BookApiFixture.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Shared;

namespace MyApp.Tests;

/// <summary>An in-memory server with the generated endpoints, plus a factory for generated clients that call it.</summary>
public sealed class BookApiFixture : IAsyncLifetime
{
    private WebApplication _app = null!;

    public BookStore Store { get; } = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton(Store);
        builder.Services.AddScoped<IBookService, InMemoryBookService>();
        builder.Services.AddRForgeAutoApiServer();   // after the registration it wraps

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapRForgeAutoApi();
        await _app.StartAsync();
    }

    /// <summary>Builds the container a WebAssembly app would have, with HttpClients that talk to the test server.</summary>
    public IBookService CreateBookService(string? user = null, string? roles = null)
    {
        var server = _app.GetTestServer();

        var services = new ServiceCollection();
        services.AddRForgeAutoApi()
            .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler())
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri("http://localhost/");
                if (user is not null) client.DefaultRequestHeaders.Add("X-Test-User", user);
                if (roles is not null) client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
            });

        return services.BuildServiceProvider().GetRequiredService<IBookService>();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();
}
```

Three things to notice:

1. **The server is set up like your app's `Program.cs`**: register the service, then `AddRForgeAutoApiServer()` after it, then `MapRForgeAutoApi()`.
2. **`EnvironmentName = "Production"`** matters. In Development the server adds the exception type, message and stack trace to unexpected-error responses, which hides the behavior you want to test. See [tutorial 04](04-errors.md).
3. **`CreateBookService` builds a WebAssembly-style container.** `AddRForgeAutoApi()` registers the generated client for `IBookService` and returns the `IHttpClientBuilder` of the default client. The test swaps its handler for the test server's, sets the base address (a `HttpClient` can't send a relative request without one) and adds the sign-in headers.

## 4. Test the round trip

```csharp
// MyApp.Tests/BookApiTests.cs
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp.Tests;

public sealed class BookApiTests(BookApiFixture fixture) : IClassFixture<BookApiFixture>
{
    [Fact]
    public async Task A_book_makes_a_round_trip()
    {
        var books = fixture.CreateBookService(user: "alice");

        var book = await books.GetBookAsync(1);

        Assert.Equal(new BookDto(1, "Dune", "Frank Herbert"), book);
    }

    [Fact]
    public async Task A_GET_method_works_without_signing_in()
    {
        var books = fixture.CreateBookService();

        Assert.NotEmpty(await books.GetAllAsync());
    }

    [Fact]
    public async Task An_expected_error_keeps_its_message_status_and_code()
    {
        var books = fixture.CreateBookService(user: "lee", roles: "Librarian");

        var ex = await Assert.ThrowsAsync<RfException>(() => books.AddBookAsync(new BookDto(0, " ", "Anon")));

        Assert.Equal("A title is required.", ex.Message);
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("title_required", ex.Code);
    }

    [Fact]
    public async Task A_missing_book_throws_RfNotFoundException()
    {
        var books = fixture.CreateBookService(user: "alice");

        var ex = await Assert.ThrowsAsync<RfNotFoundException>(() => books.GetBookAsync(99));

        Assert.Equal("book_not_found", ex.Code);
    }

    [Fact]
    public async Task An_unexpected_error_hides_its_details()
    {
        var books = fixture.CreateBookService(user: "alice");
        fixture.Store.FailWith = new InvalidOperationException("connection string is xyz");
        try
        {
            var ex = await Assert.ThrowsAsync<RfApiException>(() => books.GetBookAsync(1));

            Assert.Equal(500, ex.StatusCode);
            Assert.Null(ex.Detail);
            Assert.DoesNotContain("xyz", ex.Message);
        }
        finally
        {
            fixture.Store.FailWith = null;
        }
    }

    [Fact]
    public async Task Signing_in_is_required()
    {
        var books = fixture.CreateBookService();   // no user header

        await Assert.ThrowsAsync<RfUnauthorizedException>(() => books.GetBookAsync(1));
    }

    [Fact]
    public async Task The_Librarian_role_is_required_to_add_a_book()
    {
        var reader = fixture.CreateBookService(user: "alice", roles: "Reader");
        var librarian = fixture.CreateBookService(user: "lee", roles: "Librarian");
        var book = new BookDto(0, "Emma", "Jane Austen");

        await Assert.ThrowsAsync<RfForbiddenException>(() => reader.AddBookAsync(book));
        var added = await librarian.AddBookAsync(book);

        Assert.True(added.Id > 0);
    }
}
```

What each test pins down:

| Test | The rule it guards |
|---|---|
| Round trip | Arguments and results survive serialization, including `BookDto` and `int`. |
| GET without signing in | `[RfAllowAnonymous]` on a method overrides `[RfAuthorize]` on the interface. |
| Expected error | An `RfException` arrives with the same `Message`, `StatusCode` and `Code`. |
| Missing book | `RfNotFoundException` keeps its own type across the wire. |
| Unexpected error | Anything else becomes an `RfApiException` (500) with no internals leaked. |
| Signing in | A missing user gives `RfUnauthorizedException` (401). |
| Role | A signed-in user without the role gives `RfForbiddenException` (403). |

The tests assert on exception types and properties, not on status codes or JSON. That is the contract your components rely on. [Tutorial 04](04-errors.md) covers the exceptions and [tutorial 05](05-authorization.md) the authorization rules.

## 5. Test Interactive Server mode

In Interactive Server mode and during prerendering, components call the real service directly. No HTTP request is made, so the endpoint's authorization never runs. `AddRForgeAutoApiServer()` wraps the service in a decorator that applies the same rules. Test that too:

```csharp
// MyApp.Tests/ServerModeTests.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp.Tests;

public sealed class ServerModeTests
{
    // Interactive Server and prerendering call the real service directly, with no HTTP request.
    // AddRForgeAutoApiServer wraps it so the same authorization rules apply.
    private static IBookService BookServiceFor(ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
        services.AddSingleton<BookStore>();
        services.AddScoped<IBookService, InMemoryBookService>();
        services.AddRForgeAutoApiServer();

        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IBookService>();
    }

    private static ClaimsPrincipal SignedIn(params string[] roles) =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "bob") }.Concat(roles.Select(r => new Claim(ClaimTypes.Role, r))),
            authenticationType: "circuit"));

    [Fact]
    public async Task An_anonymous_user_is_rejected()
    {
        var books = BookServiceFor(new ClaimsPrincipal(new ClaimsIdentity()));

        await Assert.ThrowsAsync<RfUnauthorizedException>(() => books.GetBookAsync(1));
        Assert.NotEmpty(await books.GetAllAsync());   // [RfAllowAnonymous]
    }

    [Fact]
    public async Task A_user_without_the_role_is_forbidden()
    {
        var books = BookServiceFor(SignedIn("Reader"));

        await Assert.ThrowsAsync<RfForbiddenException>(() => books.AddBookAsync(new BookDto(0, "Emma", "Jane Austen")));
    }

    private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
```

Inside a Blazor circuit, the decorator reads the current user from `AuthenticationStateProvider`, so the test supplies a fixed one. The exception types are the same as in the round-trip tests: a component's `catch` block doesn't care which mode it runs in.

## 6. Test components with a fake service

A component that injects `IBookService` doesn't know whether the implementation is the real service, the generated client or a fake. In a component test, register a fake and the generated code, HTTP and the server are all out of the picture.

First, a component to test:

```razor
@* MyApp.Client/Features/Books/BookList.razor *@
@using MyApp.Shared
@using RForge.Blazor.AutoApi
@inject IBookService Books

@if (_error is not null)
{
    <p class="error">@_error</p>
}
else if (_books is null)
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
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _books = await Books.GetAllAsync();
        }
        catch (RfException ex)
        {
            _error = ex.Message;
        }
    }
}
```

Then the tests, with a hand-written fake that returns a list or throws what you tell it to:

```csharp
// MyApp.Tests/BookListTests.cs
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Client;
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp.Tests;

/// <summary>A hand-written stand-in for IBookService. No HTTP, no server, no generated code involved.</summary>
public sealed class FakeBookService : IBookService
{
    public List<BookDto> Books { get; set; } = [];
    public Exception? Failure { get; set; }

    public Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => Failure is null ? Task.FromResult(Books) : Task.FromException<List<BookDto>>(Failure);

    public Task<BookDto?> GetBookAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Books.FirstOrDefault(b => b.Id == id));

    public Task<BookDto> AddBookAsync(BookDto book, CancellationToken cancellationToken = default)
    {
        Books.Add(book);
        return Task.FromResult(book);
    }
}

public sealed class BookListTests : BunitContext
{
    private readonly FakeBookService _books = new();

    public BookListTests() => Services.AddSingleton<IBookService>(_books);

    [Fact]
    public void Lists_the_books()
    {
        _books.Books = [new(1, "Dune", "Frank Herbert"), new(2, "Emma", "Jane Austen")];

        var cut = Render<BookList>();

        Assert.Equal(["Dune by Frank Herbert", "Emma by Jane Austen"], cut.FindAll("li").Select(li => li.TextContent));
    }

    [Fact]
    public void Shows_an_expected_error_message()
    {
        _books.Failure = new RfException("The library is closed.", 503, "closed");

        var cut = Render<BookList>();

        Assert.Equal("The library is closed.", cut.Find("p.error").TextContent);
    }
}
```

- You don't need `AddRForgeAutoApi()` or `AddRForgeAutoApiServer()` here. They would only matter if the test wanted the generated code, and for component tests it doesn't.
- To test how a component handles any failure, make the fake throw the matching exception: `RfException` for an expected error, `RfUnauthorizedException` for a lost session, and so on.
- Use a mocking library instead of the hand-written fake if you prefer. The only requirement is that the container resolves `IBookService` to something.

## How it works

- The generated endpoints and the generated client come from the same interface, so a round-trip test exercises both ends of the contract.
- `TestServer` runs the whole ASP.NET pipeline in memory: routing, authentication, authorization, the endpoint filter that turns exceptions into ProblemDetails. Only the network is missing.
- Sign-in headers and the test auth scheme stand in for your real identity setup. The `[RfAuthorize]` rules run exactly as they do in production.

## Best practices

- Test the round trip once per interface, and cover each exception type and each authorization rule you rely on. These tests are short and catch the mistakes that only show up in WebAssembly.
- Also test Interactive Server mode for any interface with `[RfAuthorize]`. It is a different code path (the decorator) with the same promise.
- Use fakes in component tests. A component test that starts a server is a slow integration test.
- Keep `EnvironmentName = "Production"` in the fixture, so unexpected errors are tested the way users see them.
- Assert on exception types and `Code`, not on message text you don't control.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `InvalidOperationException: An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.` | The test client has no base address. | Set `client.BaseAddress` in `ConfigureHttpClient`, as in the fixture. |
| `InvalidOperationException: 'MyApp.Shared.IBookService' is not registered. Register your implementation before calling AddRForgeAutoApiServer().` | `AddRForgeAutoApiServer()` ran before the service was registered. | Register the service first. |
| Build error `'IServiceCollection' does not contain a definition for 'AddRForgeAutoApi'` | The test project has no generator reference, or is missing the marker class or its namespace. | Add the `RForge.Blazor.AutoApi.Generator` package to the test project, declare the marker classes, and import their namespace. |
| A test expects a Forbidden error but gets `RfUnauthorizedException` | The request had no user. 401 means "not signed in", 403 means "signed in without permission". | Pass a `user` as well as `roles` to `CreateBookService`. |

[Troubleshooting](10-troubleshooting.md) lists every build diagnostic and runtime problem.
