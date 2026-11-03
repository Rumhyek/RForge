using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RForge.Blazor.AutoApi.IntegrationTests.Shared;

namespace RForge.Blazor.AutoApi.IntegrationTests;

[RForgeAutoApiHost(typeof(IOrderService))]
public static partial class TestEndpoints { }

[RForgeAutoApiClient(typeof(IOrderService))]
public static partial class TestClients { }

public sealed class OrderService : IOrderService
{
    public static readonly List<(OrderDto Order, string? Note)> Saved = new();

    public Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<OrderDto?>(id == 404 ? null : new OrderDto(id, $"Order {id}", 12.5m, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)));

    public Task SaveOrderAsync(OrderDto order, string? note = null, CancellationToken cancellationToken = default)
    {
        lock (Saved) Saved.Add((order, note));
        return Task.CompletedTask;
    }

    public Task<string> QueryAsync(string? nameContains, OrderStatus status = OrderStatus.Open, int minTotal = 0, DateOnly? since = null)
        => Task.FromResult($"{nameContains ?? "<null>"}|{status}|{minTotal}|{since?.ToString("O") ?? "<null>"}");

    public Task<string> EchoAsync(string value) => Task.FromResult(value);

    public Task<string> EchoAsync(string value, int times) => Task.FromResult(string.Concat(Enumerable.Repeat(value, times)));

    public ValueTask<List<OrderDto>> SearchAsync(OrderFilter filter, int page)
        => ValueTask.FromResult(new List<OrderDto> { new(page, filter.NameContains ?? "", filter.MinTotal ?? 0, null) });

    public ValueTask DeleteAsync(Guid id, OrderStatus status)
        => id == Guid.Empty ? throw new RfException("Id is required.", 422, "id_required") : ValueTask.CompletedTask;

    public Task FailExpectedAsync(string code) => throw new RfException("That did not work.", 409, code);

    public Task FailNotFoundAsync(string code) => throw new RfNotFoundException("Order not found.", code);

    public Task FailUnexpectedAsync() => throw new InvalidOperationException("secret internals");

    public Task<string> PingAsync() => Task.FromResult("pong");

    public Task<bool> TaggedAsync() => Task.FromResult(true);
}

public sealed class ReportService : IReportService
{
    public Task<decimal> TotalAsync() => Task.FromResult(99.95m);
}

/// <summary>Authenticates from the X-Test-User / X-Test-Roles headers.</summary>
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user)) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.Name, user.ToString()) };
        if (Request.Headers.TryGetValue("X-Test-Roles", out var roles))
            claims.AddRange(roles.ToString().Split(',').Select(r => new Claim(ClaimTypes.Role, r)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

public sealed class ServerFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddRForgeAutoApiServer();

        App = builder.Build();
        App.UseAuthentication();
        App.UseAuthorization();
        App.MapRForgeAutoApi();
        await App.StartAsync();
    }

    /// <summary>Builds a WASM-style client container whose HttpClients talk to the test server.</summary>
    public ServiceProvider CreateClient(string? user = null, string? roles = null)
    {
        var server = App.GetTestServer();
        void Configure(HttpClient client)
        {
            client.BaseAddress = new Uri("http://localhost/");
            if (user is not null) client.DefaultRequestHeaders.Add("X-Test-User", user);
            if (roles is not null) client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        }

        var services = new ServiceCollection();
        services.AddRForgeAutoApi()
            .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler())
            .ConfigureHttpClient(Configure);
        services.AddHttpClient("ReportsApi")
            .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler())
            .ConfigureHttpClient(Configure);
        return services.BuildServiceProvider();
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}
