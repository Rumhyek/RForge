# 04 · Errors

**Goal:** throw errors from a service and catch them in a component, with the same `catch` blocks working in Interactive Server and in WebAssembly.

This tutorial assumes the projects from [tutorial 01](01-setup.md). The examples are services that fail on purpose.

## 1. Throw an expected error

An `RfException` is an *expected* error: something went wrong in a way you anticipated and the message is safe to show to a user. Throw it from your service:

```csharp
// MyApp/BookService.cs
using MyApp.Shared;
using RForge.Blazor.AutoApi;

namespace MyApp;

public sealed class BookService : IBookService
{
    private static readonly BookDto[] Books = [new(1, "Dune", "Frank Herbert"), new(2, "Hyperion", "Dan Simmons")];

    public Task<BookDto> GetBookAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Books.FirstOrDefault(b => b.Id == id)
            ?? throw new RfNotFoundException($"Book {id} was not found.", code: "book_not_found"));

    public Task<BookDto> AddAsync(string title, string author)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new RfException("A title is required.", code: "title_required");

        return Task.FromResult(new BookDto(3, title.Trim(), author));
    }

    // ...
}
```

`RfException` takes a message, a status code and an optional code:

```csharp
new RfException(message, statusCode: 400, code: null)
```

| Argument | Use |
|---|---|
| `message` | Text for the user. It is sent to the client, so never put secrets or internal details in it. |
| `statusCode` | The HTTP status. Default `400`. |
| `code` | A short, stable string your components can switch on, such as `"title_required"`. Optional. |

The library has three ready-made subclasses:

| Type | Status | Use for |
|---|---|---|
| `RfUnauthorizedException` | 401 | The user isn't signed in |
| `RfForbiddenException` | 403 | The user is signed in but not allowed |
| `RfNotFoundException` | 404 | The requested item doesn't exist |

Each takes an optional message and code. Without a message they use a default such as `You are not allowed to perform this action.` ([Tutorial 05](05-authorization.md) shows where the library throws the first two for you.)

## 2. Catch it in a component

```razor
@* MyApp.Client/Pages/BookDetails.razor *@
@page "/books/{Id:int}"
@rendermode InteractiveAuto
@using MyApp.Shared
@using RForge.Blazor.AutoApi
@inject IBookService Books
@inject ILogger<BookDetails> Logger

@if (error is not null)
{
    <div class="notification is-warning">@error</div>
}
else if (book is not null)
{
    <p>@book.Title by @book.Author</p>
}

@code {
    [Parameter]
    public int Id { get; set; }

    private BookDto? book;
    private string? error;

    protected override async Task OnParametersSetAsync()
    {
        try
        {
            error = null;
            book = await Books.GetBookAsync(Id);
        }
        catch (RfNotFoundException)
        {
            error = "That book doesn't exist.";
        }
        catch (RfException ex)
        {
            // Expected errors: the message is safe to show, in Server and in WebAssembly.
            error = ex.Message;
        }
        catch (OperationCanceledException)
        {
            // The user navigated away. Not an error.
        }
        catch (Exception ex)
        {
            // Unexpected: RfApiException in WebAssembly, the original exception in Server.
            Logger.LogError(ex, "Loading book {Id} failed", Id);
            error = "Something went wrong. Please try again.";
        }
    }
}
```

Order the `catch` blocks from the most specific to the least: `RfNotFoundException`, then `RfException`, then everything else. The same page behaves the same whether it runs prerendered, in Interactive Server or in WebAssembly.

## 3. What travels over the wire

In WebAssembly the exception crosses HTTP as a `ProblemDetails` JSON body. For `GetBookAsync(99)`:

```
HTTP/1.1 404 Not Found

{
  "title": "Book 99 was not found.",
  "status": 404,
  "traceId": "00-2d600eff7f71486332e0e179f224f4ef-033d641b56e79653-00",
  "rfType": "RfException",
  "code": "book_not_found"
}
```

| Field | Meaning |
|---|---|
| `status` | The exception's `StatusCode` |
| `title` | The exception's `Message` |
| `code` | The exception's `Code`, left out when it is null |
| `rfType` | Always `"RfException"`. It tells the client "this is a service error, not a crash". |
| `traceId` | Correlates the response with the server logs |

The generated client turns the response back into an exception:

| Response | The client throws |
|---|---|
| `401` | `RfUnauthorizedException` |
| `403` | `RfForbiddenException` |
| `404` with an `rfType` | `RfNotFoundException` |
| any status with `rfType` `"RfException"` | `RfException` with the same `Message`, `StatusCode` and `Code` |
| anything else | `RfApiException` (see section 5) |

A 401 or 403 always becomes the matching type, whatever the body contains.

## 4. Custom exception types

You can subclass `RfException` to give an error a name in your own code:

```csharp
// MyApp.Shared/BookLockedException.cs
using RForge.Blazor.AutoApi;

namespace MyApp.Shared;

public sealed class BookLockedException(int id)
    : RfException($"Book {id} is locked by another user.", statusCode: 409, code: "book_locked");
```

The type only survives *inside the server*. In Interactive Server mode the component calls your service directly, so it catches a `BookLockedException`. In WebAssembly only the message, status and code cross the wire, so the client throws a plain `RfException`:

| Mode | A component sees |
|---|---|
| Interactive Server (and prerendering) | `BookLockedException`, message `Book 1 is locked by another user.`, `StatusCode` 409, `Code` `book_locked` |
| WebAssembly | `RfException`, the same message, `StatusCode` 409 and `Code` `book_locked` |

That's why the examples catch `RfException` and look at `Code`, not at the subclass. Only `RfUnauthorizedException`, `RfForbiddenException` and `RfNotFoundException` are rebuilt by type on the client:

```csharp
// MyApp.Client/Pages/Reserve.razor, inside the @code block (a catch that works in both modes)
catch (RfException ex) when (ex.Code == "book_locked")
{
    message = "Someone else is editing this book.";
}
```

## 5. Unexpected errors

Any other exception is a bug or an outage, not an expected outcome. The server logs it (category `RForge.Blazor.AutoApi`, level `Error`) and answers `500` with a generic message:

```
HTTP/1.1 500 Internal Server Error

{"title":"An unexpected error occurred.","status":500,"traceId":"00-b7e78a07aa6c523f03a509c4bd1cea19-439c4db45d8a24c2-00"}
```

In the **Development** environment the body also has a `detail` field with the exception's type, message and stack trace. In every other environment it doesn't, so nothing internal leaks.

The client turns a `500`, or any other response that isn't a service error, into an `RfApiException`:

| Property | Value |
|---|---|
| `StatusCode` | The HTTP status |
| `Title` | The ProblemDetails title, or the response's reason phrase when there is none |
| `Detail` | The ProblemDetails `detail`. Only filled in Development. |
| `TraceId` | The server's trace id, for finding the log entry |

A wrong URL, a gateway error or a proxy's HTML error page also arrive as an `RfApiException`, with the status code of that response.

In **Interactive Server** mode there is no HTTP and no conversion: the original exception propagates unchanged, here an `InvalidOperationException("The database is on fire.")`. Your `catch (Exception)` block gets an `RfApiException` in WebAssembly and the original exception on the server, which is why it should log and show a generic message rather than anything from `ex.Message`.

## 6. Cancellation and network failures

- **Cancellation is never converted.** If the token you pass to a call is canceled, the client throws `TaskCanceledException`, which derives from `OperationCanceledException`, and the server stops the work: its `CancellationToken` parameter is `RequestAborted`. The server doesn't log a canceled request as an error or answer it. Catch `OperationCanceledException` separately, as in section 2, so navigation doesn't show an error.
- **Network failures pass through.** If the server is unreachable, the client throws `HttpRequestException` unchanged, because there is no response to convert. Catch it if you want a specific offline message. In Server mode there is no network, so it can't happen.

## 7. Errors from the generated code itself

The generated endpoints report their own binding problems as `RfException` with status `400`, so they go through the same `catch` as your errors:

| Cause | `RfException` message |
|---|---|
| A required parameter is missing | `Missing required parameter 'filter'.` |
| A value can't be read as its type | `Invalid value for parameter 'page'.` |
| The body isn't valid JSON | `The request body is not valid JSON.` |

[Tutorial 03](03-parameters-and-serialization.md) explains when each one happens.

## How it works

- The generated endpoint wraps your method in a `try`/`catch`. An `RfException` becomes a ProblemDetails body with its status code; any other exception becomes the `500` above; a canceled request is dropped.
- The generated client reads the status code and the `rfType` and `code` fields, and throws the matching exception.
- In Interactive Server mode no endpoint is involved, so exceptions are just the exceptions your service throws.

## Best practices

- **Use `RfException` for outcomes you expect** (validation, not found, conflicts) and let everything else throw normally.
- **Write messages for users.** They travel to the browser exactly as written.
- **Give errors a `code`** when a component needs to react differently. A string compares the same in both modes, unlike a subclass.
- **Catch specific types first**, then `RfException`, then `Exception`.
- **Log unexpected errors in the component** with the trace id when you have an `RfApiException`, and show a generic message.
- **Never put sensitive data in an exception message** or in `Code`.

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| `catch (BookLockedException)` works on the server but not in WebAssembly | Custom subclasses become plain `RfException` on the client | Catch `RfException` and check `Code` |
| Users see "An unexpected error occurred." | A non-`RfException` was thrown in the service | Throw `RfException` for expected cases, or fix the bug and read the log |
| The 500 body has no stack trace | The app isn't running in the Development environment | That is intended. Use the log and the `traceId` |
| An error banner appears when the user navigates away | `OperationCanceledException` is caught by `catch (Exception)` | Catch `OperationCanceledException` first |
| `ex.Message` shows internal text in the browser | `RfApiException` or an unexpected exception message is displayed | Show your own text for unexpected errors |
| Every call throws `RfApiException` with status 400 or 404 and a plain reason phrase such as `Bad Request` | The client's `HttpClient` base address points at the wrong place, so the request never reaches the generated endpoints | Check the base address and any path in it ([tutorial 07](07-httpclient-setup.md)) |

**Next:** [05 · Authorization](05-authorization.md)
