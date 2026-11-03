namespace RForge.Blazor.AutoApi;

/// <summary>
/// An expected error whose <see cref="Exception.Message"/> is safe to show to users.
/// Thrown the same way in Interactive Server and WebAssembly render modes.
/// </summary>
public class RfException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">A user-safe message.</param>
    /// <param name="statusCode">HTTP status code used on the wire. Default 400.</param>
    /// <param name="code">Optional application-specific error code.</param>
    /// <param name="innerException">Optional inner exception (never sent to clients).</param>
    public RfException(string message, int statusCode = 400, string? code = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
    }

    /// <summary>HTTP status code used on the wire.</summary>
    public int StatusCode { get; }

    /// <summary>Optional application-specific error code.</summary>
    public string? Code { get; }
}

/// <summary>The current user is not authenticated (401).</summary>
public class RfUnauthorizedException(string? message = null, string? code = null)
    : RfException(message ?? "You must be signed in to perform this action.", 401, code);

/// <summary>The current user is authenticated but not authorized (403).</summary>
public class RfForbiddenException(string? message = null, string? code = null)
    : RfException(message ?? "You are not allowed to perform this action.", 403, code);

/// <summary>The requested item does not exist or is not available to the current user (404).</summary>
public class RfNotFoundException(string? message = null, string? code = null)
    : RfException(message ?? "The requested item was not found.", 404, code);

/// <summary>
/// An unexpected failure returned by an Auto API endpoint. Only thrown by generated WebAssembly clients;
/// in Interactive Server mode the original exception propagates instead.
/// </summary>
public class RfApiException(int statusCode, string? title, string? detail = null, string? traceId = null)
    : Exception(BuildMessage(statusCode, title, detail))
{
    /// <summary>HTTP status code of the response.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>ProblemDetails title, when present.</summary>
    public string? Title { get; } = title;

    /// <summary>ProblemDetails detail. Includes exception details only in the Development environment.</summary>
    public string? Detail { get; } = detail;

    /// <summary>Server trace identifier, useful for correlating logs.</summary>
    public string? TraceId { get; } = traceId;

    private static string BuildMessage(int statusCode, string? title, string? detail)
    {
        var message = $"Auto API request failed with status {statusCode}";
        if (!string.IsNullOrEmpty(title)) message += $": {title}";
        if (!string.IsNullOrEmpty(detail)) message += $"{Environment.NewLine}{detail}";
        return message;
    }
}
