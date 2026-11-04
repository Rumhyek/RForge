# 11 · Best practices

A one-page checklist of the recommendations from the tutorials. Each item links to the tutorial that explains it.

## Project layout (recommended)

This is the layout the tutorials use. It is a recommendation, not a requirement. The things that are required are called out in [tutorial 01](01-setup.md).

```
MyApp.Shared/                           references RForge.Blazor.AutoApi.Abstractions only, no ASP.NET
├── I<Feature>Service.cs                [RForgeAutoApi] interfaces
├── <Feature>Dto.cs                     the types the interfaces send and return
└── AppJsonContext.cs                   the JsonSerializerContext, plus [assembly: RForgeAutoApiDefaults(...)]
MyApp/                                  server project (references the Generator package)
├── Program.cs                          register services, AddRForgeAutoApiServer(), MapRForgeAutoApi()
├── AutoApiEndpoints.cs                 the [RForgeAutoApiHost] class
└── Services/<Feature>Service.cs        the real implementations
MyApp.Client/                           WebAssembly project (references the Generator package)
├── Program.cs                          AddRForgeAutoApi()
├── AutoApiClients.cs                   the [RForgeAutoApiClient] class
└── Features/…                          components that inject the interfaces
MyApp.Tests/                            round-trip, server-mode and component tests
```

## Where each file goes

| File | Put it in | Why | Tutorial |
|---|---|---|---|
| Interfaces, DTOs, `JsonSerializerContext` | `MyApp.Shared` | Both the server and the client need them, and the generators read them from a referenced assembly | [01](01-setup.md), [03](03-parameters-and-serialization.md) |
| `[assembly: RForgeAutoApiDefaults]` | `MyApp.Shared` | Both generators read it, so the server and client always agree on prefix, default method and JSON context | [01](01-setup.md), [03](03-parameters-and-serialization.md) |
| `[RForgeAutoApiHost]` class | The server project | It generates ASP.NET code | [01](01-setup.md) |
| `[RForgeAutoApiClient]` class | The WebAssembly project | It generates the `HttpClient` implementation | [01](01-setup.md) |
| Generator package reference | Every project that has a marker class | NuGet doesn't pass analyzers through project references | [01](01-setup.md), [10](10-troubleshooting.md) |
| The real services | The server project, registered before `AddRForgeAutoApiServer()` | Server mode and prerendering call them directly | [05](05-authorization.md), [06](06-render-modes.md) |
| Handlers and client names | `MyApp.Client/Program.cs`, on the builder `AddRForgeAutoApi()` returns | One place to add tokens and extra clients | [07](07-httpclient-setup.md) |
| Rate limiting and other endpoint conventions | `Program.cs`, chained on `MapRForgeAutoApi()` | They apply to every generated endpoint | [08](08-endpoint-conventions.md) |
| Tests | `MyApp.Tests` | One fixture shares an in-memory server | [09](09-testing.md) |

## Naming

| Thing | Convention | Example |
|---|---|---|
| Interface | `I<Feature>Service` | `IBookService` |
| Method | Verb, ends in `Async` | `GetBookAsync`, `AddBookAsync` |
| Data type | `<Noun>Dto` | `BookDto` |
| Marker classes | `AutoApiEndpoints` and `AutoApiClients` | |
| Error `code` | Lower snake case, one per reason | `title_required`, `book_not_found` |

## Interfaces and methods

- [ ] Every method returns `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`. No generics, no `ref`/`out`/`in`, no properties or events. ([02](02-methods-and-routes.md), [10](10-troubleshooting.md))
- [ ] Remember that `[RForgeAutoApi]` opts in **every** method of the interface. Split an interface if some methods should not be callable from the browser. ([02](02-methods-and-routes.md))
- [ ] Keep the default `Post` unless a method is a safe read. Use `Get` and `Delete` only with simple parameters. ([02](02-methods-and-routes.md), [03](03-parameters-and-serialization.md))
- [ ] Take a `CancellationToken` (at most one) and pass it on to your async calls. ([03](03-parameters-and-serialization.md))
- [ ] Avoid overloads, or give each its own `Route`. Default routes for overloads are numbered in declaration order. ([02](02-methods-and-routes.md))
- [ ] Add new parameters with a default value. Adding one is not a breaking change. ([03](03-parameters-and-serialization.md))

## Serialization

- [ ] Declare one `JsonSerializerContext` in the shared library and point `RForgeAutoApiDefaults.JsonContext` at it. ([03](03-parameters-and-serialization.md))
- [ ] List every parameter and return type in it, including `List<T>` and simple types such as `int`. ([03](03-parameters-and-serialization.md))
- [ ] Treat `RFAA009` as an error in CI, so a missing type can't fall back to reflection unnoticed. ([10](10-troubleshooting.md))

## Errors

- [ ] Throw `RfException` (with a `code`) for expected failures, and `RfNotFoundException` for a missing item. ([04](04-errors.md))
- [ ] Write `RfException` messages you are happy to show a user. They are sent to the browser. ([04](04-errors.md))
- [ ] In components, `catch (RfException)` to show the message, and log everything else. The same `catch` works in every render mode. ([04](04-errors.md))
- [ ] Don't catch `Exception` to hide failures. Unexpected errors arrive as `RfApiException` in WebAssembly, with the details left on the server. ([04](04-errors.md))

## Authorization

- [ ] Put `[RfAuthorize]` on the interface and `[RfAllowAnonymous]` on the few methods that are public. ([05](05-authorization.md))
- [ ] Register services that have authorization rules as Scoped or Transient. A Singleton throws at startup. ([05](05-authorization.md), [10](10-troubleshooting.md))
- [ ] Call `AddRForgeAutoApiServer()` after all your service registrations. ([05](05-authorization.md), [10](10-troubleshooting.md))
- [ ] Remember that Interactive Server mode makes no HTTP call. The decorator, not the endpoint, enforces the rules there. ([05](05-authorization.md), [06](06-render-modes.md))

## Components and render modes

- [ ] Inject the interface and nothing else. Don't check the render mode in the component. ([06](06-render-modes.md))
- [ ] Don't assume a call is cheap or free of serialization. In WebAssembly it is an HTTP request. ([06](06-render-modes.md))

## HttpClient and endpoints

- [ ] Add handlers (for example a token handler) to the builder `AddRForgeAutoApi()` returns. ([07](07-httpclient-setup.md))
- [ ] Use `HttpClientName` only when an interface really talks to a different host. ([07](07-httpclient-setup.md))
- [ ] Chain rate limiting and other conventions onto `MapRForgeAutoApi()`. ([08](08-endpoint-conventions.md))

## Deploying

- [ ] Deploy the server and the WebAssembly client from the same build. The routes are generated from the same source, so mixed versions can disagree. ([02](02-methods-and-routes.md), [10](10-troubleshooting.md))

## Testing

- [ ] One round-trip fixture with `TestServer` and a test auth scheme. Cover each exception type and each authorization rule. ([09](09-testing.md))
- [ ] Test Interactive Server mode for any interface with `[RfAuthorize]`. ([09](09-testing.md))
- [ ] Test components with a fake of the interface, not with a server. ([09](09-testing.md))
- [ ] Run the fixture with `EnvironmentName = "Production"`, so unexpected errors are tested the way users see them. ([09](09-testing.md))

## Build settings

- [ ] Set `<WarningsAsErrors>$(WarningsAsErrors);RFAA009;RFAA010</WarningsAsErrors>` so the two warnings fail the build. ([10](10-troubleshooting.md))
- [ ] When something looks wrong, set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` and read the generated endpoints and client. ([10](10-troubleshooting.md))
