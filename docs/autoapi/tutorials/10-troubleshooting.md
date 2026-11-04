# 10 · Troubleshooting

**Goal:** find the cause of a build diagnostic, a compiler error or a runtime failure quickly. The tables list every `RFAA` diagnostic and the problems seen most often, each with a fix. The explanations are in the other tutorials, which the tables link to.

## Build diagnostics

The generator reports these. Errors stop the build. Warnings and info point at likely bugs.

| ID | Severity | Message (short) | Fix |
|---|---|---|---|
| `RFAA001` | Error | `'X' must be a non-nested, non-generic 'static partial' class` | Declare the marker class as `public static partial class X { }` at the top level of a namespace, not nested and not generic ([tutorial 01](01-setup.md)). |
| `RFAA002` | Warning | `Assembly 'X' contains no accessible [RForgeAutoApi] interfaces` | The type you passed to `[RForgeAutoApiHost(typeof(…))]` or `[RForgeAutoApiClient(typeof(…))]` must come from the assembly that holds your interfaces. Pass one of your `[RForgeAutoApi]` interfaces, and check the interfaces are accessible (`public`). |
| `RFAA003` | Error | `'M' returns 'T'; only Task, Task<T>, ValueTask and ValueTask<T> are supported` | Make the method async: return `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>` ([tutorial 02](02-methods-and-routes.md)). |
| `RFAA004` | Error | `'M' is generic; generic interfaces and generic methods are not supported` | Use concrete types. Replace a generic method with one method per type. A generic method also reports `RFAA009` for its type parameter; fix `RFAA004` first. |
| `RFAA005` | Error | `Parameter 'p' of 'M' is ref/out/in` | Pass the value normally and return the result, using a record if you have several ([tutorial 03](03-parameters-and-serialization.md)). |
| `RFAA006` | Error | `'M' uses GET but parameter 'p' of type 'T' is not a simple type` | `Get` and `Delete` send parameters in the query string, so they take simple types only. Use the default `Post`, or pass simple values ([tutorial 03](03-parameters-and-serialization.md)). Also reported for `DELETE`. |
| `RFAA007` | Error | `Custom route 'r' on 'M' contains a placeholder` | Routes are static. Remove `{id}` from `Route`, and let the method take `id` as a parameter ([tutorial 02](02-methods-and-routes.md)). |
| `RFAA008` | Error | `Route 'r' is used by more than one method` | Give one of the two methods a different `Route`, or remove the custom route and let the generator number the overloads ([tutorial 02](02-methods-and-routes.md)). |
| `RFAA009` | Warning | `'T' is not registered in 'Ctx'; reflection-based serialization will be used for it` | Add `[JsonSerializable(typeof(T))]` to the context, and keep the context in the **shared library** (see the runtime table below). Without it the type still works, through slower reflection that isn't trim-safe ([tutorial 03](03-parameters-and-serialization.md)). |
| `RFAA010` | Warning | `Attribute 'A' on 'M' could not be reconstructed and was not copied to the endpoint` | The generator copies attributes from the interface onto the endpoint, and the host project can't name this one. The usual cause is an `internal` attribute declared in the shared library. Make it `public`, or list it in `ExcludedAttributes` if it shouldn't reach the endpoint ([tutorial 08](08-endpoint-conventions.md)). |
| `RFAA011` | Error | `'M' has more than one CancellationToken parameter` | Keep one. It is never sent: the client passes it to `HttpClient`, the server uses `RequestAborted` ([tutorial 03](03-parameters-and-serialization.md)). |
| `RFAA012` | Info | `No JsonContext is configured for assembly 'X'; using reflection-based serialization` | Add `[assembly: RForgeAutoApiDefaults(JsonContext = typeof(AppJsonContext))]` to the shared library ([tutorial 03](03-parameters-and-serialization.md)). This is only info, so it doesn't show in the console build output; look for it in your IDE's error list. |
| `RFAA013` | Error | `'I' declares 'm'; [RForgeAutoApi] interfaces may only contain instance methods` | Remove properties, events and static members from the interface. Turn a property into a method, for example `Task<int> GetCountAsync()`. |
| `RFAA014` | Error | `Only one [RForgeAutoApiHost] class is allowed per project; 'X' is ignored` | Keep one host class and one client class per project. A single marker covers every interface in the assemblies of the types you pass to it. |

Raise a warning to an error in CI so it can't be missed:

```xml
<!-- MyApp/MyApp.csproj -->
<PropertyGroup>
  <WarningsAsErrors>$(WarningsAsErrors);RFAA009;RFAA010</WarningsAsErrors>
</PropertyGroup>
```

## Compiler errors you may see

| Error | Meaning and fix |
|---|---|
| `CS1061: 'IServiceCollection' does not contain a definition for 'AddRForgeAutoApi'` (or `AddRForgeAutoApiServer`, or `MapRForgeAutoApi`) | One of three causes. **1.** The project has no `RForge.Blazor.AutoApi.Generator` reference: NuGet doesn't pass analyzers through project references, so add the package to the server project *and* the client project. **2.** The marker class's namespace isn't imported: the generator puts the extension methods in the marker class's namespace, so add a `using` for it in `Program.cs`. **3.** The marker class is missing, or is the wrong kind: `AddRForgeAutoApiServer` and `MapRForgeAutoApi` come from the `[RForgeAutoApiHost]` class and `AddRForgeAutoApi` from the `[RForgeAutoApiClient]` class. |
| `CS0234: The type or namespace name 'AspNetCore' does not exist in the namespace 'Microsoft'`, then `CS0246` for `HttpContext` and `IAuthorizeData`, in the generated host code | The project that holds the `[RForgeAutoApiHost]` class doesn't reference ASP.NET Core. The host class belongs in the server project (Web SDK), or add `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. |

## Runtime problems

| Symptom | Cause | Fix |
|---|---|---|
| Startup throws `InvalidOperationException: 'MyApp.Shared.IBookService' is not registered. Register your implementation before calling AddRForgeAutoApiServer().` | `AddRForgeAutoApiServer()` wraps your existing registration of each interface. It ran before the registration, or an interface was never registered. | Register the real implementation of **every** `[RForgeAutoApi]` interface, then call `AddRForgeAutoApiServer()` after all of them ([tutorial 05](05-authorization.md)). |
| Startup throws `InvalidOperationException: 'MyApp.Shared.IBookService' uses [RfAuthorize] and must be registered as Scoped or Transient, not Singleton, so authorization can see the current user.` | A service with authorization rules is registered as a Singleton. | Register it with `AddScoped` or `AddTransient`. A Singleton is fine for an interface with no `[RfAuthorize]`. |
| The client throws `RfApiException` with `Auto API request failed with status 404: Not Found` | The request didn't reach a generated endpoint. Either the server never called `MapRForgeAutoApi()`, or the client's base address points at a different host or path, or the client and the server were built from different versions of the interface and disagree on a route. | Call `app.MapRForgeAutoApi()`, check the client's `BaseAddress` and any reverse-proxy path, and deploy the server and client from the same build. A routing 404 is an `RfApiException`; a missing *item* that your service throws as `RfNotFoundException` is a different, typed exception ([tutorial 04](04-errors.md)). |
| The client throws `InvalidOperationException: An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.` | The `HttpClient` has no base address. In a WebAssembly app `AddRForgeAutoApi()` takes it from `IWebAssemblyHostEnvironment`. In any other host (tests, a console app, MAUI) nothing sets it. | Set it: `builder.Services.AddRForgeAutoApi().ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:5001/"))` ([tutorial 07](07-httpclient-setup.md)). |
| Calls fail after you reordered or inserted overloads | Duplicate default routes get a number in declaration order (`FindAsync`, `FindAsync1`, …). Reordering overloads changes which method has which route. | Deploy the server and the client together, or give each overload its own `Route` so the order stops mattering ([tutorial 02](02-methods-and-routes.md)). |
| `RFAA009` is reported for every type, although they're all in your `JsonSerializerContext` | The context is declared in the same project as the marker class. The generator reads the context from a **referenced** assembly, and a context in the same project looks empty to it. | Move the context to the shared library, next to the interfaces and the `[assembly: RForgeAutoApiDefaults]` attribute ([tutorial 03](03-parameters-and-serialization.md)). |
| Calling an endpoint by hand (curl, Postman) returns `400` with `The request body must be a JSON object.` or `Missing required parameter 'id'.` | `Post`, `Put` and `Patch` endpoints expect a JSON **object keyed by parameter name**, even for one parameter. A bare value, a different name or an empty body is rejected. | Send `{"id":1}`, not `1`. Names are matched in camelCase ([tutorial 03](03-parameters-and-serialization.md)). |
| Calling an endpoint by hand returns `405` | The method is a `Post` endpoint and you sent a `GET`. | Use the HTTP method the endpoint was generated with. It is `Post` unless `[RForgeAutoApiMethod]` says otherwise ([tutorial 02](02-methods-and-routes.md)). |
| The client throws `RfApiException` with no useful message for a bug on the server | Unexpected exceptions are turned into a generic 500 on purpose, so internals don't leak. | Log the exception on the server and read the `TraceId` on the `RfApiException`. In the Development environment the response also carries the exception details ([tutorial 04](04-errors.md)). |
| A call works in Interactive Server mode and fails in WebAssembly, or the reverse | The two modes take different paths. Server mode and prerendering call your service directly, with no HTTP and no serialization. WebAssembly serializes the arguments and result and sends them over HTTP. | Test both paths ([tutorial 09](09-testing.md)) and see [tutorial 06](06-render-modes.md). |

## Still stuck?

- **Look at the generated code.** Add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to the project that holds the marker class and build. The files appear under `obj/<configuration>/<framework>/generated/RForge.Blazor.AutoApi.Generator/`, for example `MyApp.AutoApiEndpoints.RForgeAutoApi.Server.g.cs`. They show the exact routes, the binding code and the attributes copied to each endpoint.
- Compare with the working Todos example in the [AutoApi sample](../../../samples/AutoApiSample).
- Read the [specification](../spec.md) for the full rules behind each diagnostic.
