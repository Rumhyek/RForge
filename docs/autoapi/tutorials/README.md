# RForge.Blazor.AutoApi tutorials

Step-by-step guides for using RForge.Blazor.AutoApi in a Blazor Web App. They assume you already know Blazor components, dependency injection, minimal APIs and render modes, and explain only what is specific to the generated API.

Start with **Setup**. After that, each tutorial stands alone: it has its own small example, so you can jump straight to the topic you need.

| # | Tutorial | You'll learn |
|---|---|---|
| 01 | [Setup](01-setup.md) | Install the packages, declare an interface, mark the server and the client, call it from a component |
| 02 | [Methods and routes](02-methods-and-routes.md) | Choose HTTP methods and routes, and see how return types become status codes |
| 03 | [Parameters and serialization](03-parameters-and-serialization.md) | How arguments travel, which types work in GET, and the JSON context that keeps WebAssembly fast |
| 04 | [Errors](04-errors.md) | Throw `RfException` on the server and catch it the same way in every render mode |
| 05 | [Authorization](05-authorization.md) | Protect methods with `[RfAuthorize]`, including in Interactive Server where no HTTP call is made |
| 06 | [Render modes](06-render-modes.md) | What runs where in Server, WebAssembly and Auto, and when HTTP is involved |
| 07 | [HttpClient setup](07-httpclient-setup.md) | Base addresses, message handlers for tokens and extra named clients |
| 08 | [Endpoint conventions](08-endpoint-conventions.md) | Rate limiting, antiforgery, tags and attributes on the generated endpoints |
| 09 | [Testing](09-testing.md) | Test the generated client against the generated endpoints, and fake the interface in component tests |
| 10 | [Troubleshooting](10-troubleshooting.md) | Every `RFAA` diagnostic and the most common runtime mistakes |
| 11 | [Best practices](11-best-practices.md) | One-page checklist: project layout, interfaces, errors, authorization, testing |

## Conventions used everywhere

Each code block starts with a comment giving the **file path** it belongs in, for example `// MyApp.Shared/IBookService.cs`. The tutorials use three projects: `MyApp.Shared` for the interface and its types, `MyApp` for the server, and `MyApp.Client` for WebAssembly. That layout is a recommendation, not a requirement. [Setup](01-setup.md) lists the few things that are required.

## See also

- [Todos example](../../../samples/AutoApiSample): a todo list that calls one interface from Interactive Server and from WebAssembly.
- [Specification](../spec.md): every attribute, route and diagnostic in one document.
