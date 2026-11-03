namespace RForge.Blazor.AutoApi;

/// <summary>HTTP methods an Auto API endpoint can use.</summary>
public enum RfHttpMethod
{
    /// <summary>GET. Parameters must be simple types and are sent in the query string.</summary>
    Get,
    /// <summary>POST. Parameters are sent as a JSON object body.</summary>
    Post,
    /// <summary>PUT. Parameters are sent as a JSON object body.</summary>
    Put,
    /// <summary>PATCH. Parameters are sent as a JSON object body.</summary>
    Patch,
    /// <summary>DELETE. Parameters must be simple types and are sent in the query string.</summary>
    Delete,
}

/// <summary>
/// Opts every method of the interface into Auto API generation: a minimal API endpoint on the server
/// and an <c>HttpClient</c> based implementation on the WebAssembly client.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class RForgeAutoApiAttribute : Attribute
{
    /// <summary>Named <c>HttpClient</c> used by every method of this interface. Defaults to the assembly default.</summary>
    public string? HttpClientName { get; set; }

    /// <summary>Path segment under the route prefix. Defaults to the interface name.</summary>
    public string? RouteSegment { get; set; }
}

/// <summary>Overrides the Auto API defaults for a single interface method.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RForgeAutoApiMethodAttribute : Attribute
{
    /// <summary>HTTP method for the endpoint. Defaults to the assembly default (<see cref="RfHttpMethod.Post"/>).</summary>
    public RfHttpMethod HttpMethod { get; set; }

    /// <summary>Static route relative to the route prefix. Placeholders such as <c>{id}</c> are not supported.</summary>
    public string? Route { get; set; }

    /// <summary>Named <c>HttpClient</c> for this method. Defaults to the interface setting.</summary>
    public string? HttpClientName { get; set; }
}

/// <summary>
/// Mirrors <c>Microsoft.AspNetCore.Authorization.AuthorizeAttribute</c> without requiring an ASP.NET reference.
/// Enforced on the generated endpoint and, through <c>AddRForgeAutoApiServer()</c>, in Interactive Server mode.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class RfAuthorizeAttribute : Attribute
{
    /// <summary>Creates an attribute that requires an authenticated user.</summary>
    public RfAuthorizeAttribute() { }

    /// <summary>Creates an attribute that requires the given policy.</summary>
    public RfAuthorizeAttribute(string policy) => Policy = policy;

    /// <summary>Policy name.</summary>
    public string? Policy { get; set; }

    /// <summary>Comma-separated list of roles.</summary>
    public string? Roles { get; set; }

    /// <summary>Comma-separated list of authentication schemes.</summary>
    public string? AuthenticationSchemes { get; set; }
}

/// <summary>Mirrors <c>Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute</c>.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RfAllowAnonymousAttribute : Attribute
{
}

/// <summary>
/// Marks a <c>static partial</c> class in the server project. The generator adds <c>MapRForgeAutoApi()</c>
/// and <c>AddRForgeAutoApiServer()</c> to it for every <see cref="RForgeAutoApiAttribute"/> interface
/// in the assemblies containing <paramref name="targetAssemblies"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RForgeAutoApiHostAttribute(params Type[] targetAssemblies) : Attribute
{
    /// <summary>Types whose assemblies are scanned for Auto API interfaces.</summary>
    public Type[] TargetAssemblies { get; } = targetAssemblies;
}

/// <summary>
/// Marks a <c>static partial</c> class in the WebAssembly client project. The generator adds
/// <c>AddRForgeAutoApi()</c> to it, registering an <c>HttpClient</c> implementation of every
/// <see cref="RForgeAutoApiAttribute"/> interface in the assemblies containing <paramref name="targetAssemblies"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RForgeAutoApiClientAttribute(params Type[] targetAssemblies) : Attribute
{
    /// <summary>Types whose assemblies are scanned for Auto API interfaces.</summary>
    public Type[] TargetAssemblies { get; } = targetAssemblies;
}

/// <summary>
/// Defaults shared by the server and client generators. Declare once in the library that holds the interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class RForgeAutoApiDefaultsAttribute : Attribute
{
    /// <summary>Route prefix for every endpoint. Default <c>/api/auto</c>.</summary>
    public string RoutePrefix { get; set; } = "/api/auto";

    /// <summary>HTTP method for methods that do not specify one. Default <see cref="RfHttpMethod.Post"/>.</summary>
    public RfHttpMethod DefaultHttpMethod { get; set; } = RfHttpMethod.Post;

    /// <summary>Default named <c>HttpClient</c>. Default <c>RForgeAutoApi</c>.</summary>
    public string HttpClientName { get; set; } = "RForgeAutoApi";

    /// <summary>
    /// A <c>JsonSerializerContext</c> in this assembly holding every parameter and return type.
    /// When null, reflection-based serialization with web defaults is used.
    /// </summary>
    public Type? JsonContext { get; set; }

    /// <summary>Attribute types that are never copied to generated endpoints.</summary>
    public Type[]? ExcludedAttributes { get; set; }
}
