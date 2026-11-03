using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.AutoApi.IntegrationTests.Shared;

namespace RForge.Blazor.AutoApi.IntegrationTests;

public sealed class ErrorAndAuthorizationTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    private IOrderService Orders(string? user = "alice", string? roles = null)
        => fixture.CreateClient(user, roles).GetRequiredService<IOrderService>();

    // ------------------------------------------------------------ errors over HTTP (WASM mode)

    [Fact]
    public async Task RfException_is_rethrown_with_message_status_and_code()
    {
        var ex = await Assert.ThrowsAsync<RfException>(() => Orders().FailExpectedAsync("conflict"));

        Assert.Equal("That did not work.", ex.Message);
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("conflict", ex.Code);
    }

    [Fact]
    public async Task RfException_from_query_endpoint_is_rethrown()
    {
        var ex = await Assert.ThrowsAsync<RfException>(async () => await Orders().DeleteAsync(Guid.Empty, OrderStatus.Open));

        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("id_required", ex.Code);
    }

    [Fact]
    public async Task RfNotFoundException_is_rethrown_as_typed_exception()
    {
        var ex = await Assert.ThrowsAsync<RfNotFoundException>(() => Orders().FailNotFoundAsync("order_not_found"));

        Assert.Equal("Order not found.", ex.Message);
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("order_not_found", ex.Code);
    }

    [Fact]
    public async Task Unexpected_exception_becomes_RfApiException_without_details_outside_development()
    {
        var ex = await Assert.ThrowsAsync<RfApiException>(() => Orders().FailUnexpectedAsync());

        Assert.Equal(500, ex.StatusCode);
        Assert.Null(ex.Detail);
        Assert.DoesNotContain("secret internals", ex.Message);
        Assert.NotNull(ex.TraceId);
    }

    // ------------------------------------------------------------ authorization over HTTP (WASM mode)

    [Fact]
    public async Task Interface_level_authorize_rejects_anonymous_user()
    {
        await Assert.ThrowsAsync<RfUnauthorizedException>(() => Orders(user: null).GetOrderAsync(1));
    }

    [Fact]
    public async Task Method_level_role_requirement_rejects_user_without_role()
    {
        await Assert.ThrowsAsync<RfForbiddenException>(() => Orders(roles: "Viewer").SaveOrderAsync(new OrderDto(1, "x", 1m, null)));
    }

    [Fact]
    public async Task AllowAnonymous_overrides_interface_authorize()
    {
        Assert.Equal("hi", await Orders(user: null).EchoAsync("hi"));
    }

    // ------------------------------------------------------------ authorization in Interactive Server mode

    private static IOrderService ServerModeOrders(ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddRForgeAutoApiServer();
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IOrderService>();
    }

    private static ClaimsPrincipal User(params string[] roles)
        => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "bob") }.Concat(roles.Select(r => new Claim(ClaimTypes.Role, r))), "circuit"));

    [Fact]
    public async Task Server_mode_decorator_rejects_anonymous_user()
    {
        var orders = ServerModeOrders(new ClaimsPrincipal(new ClaimsIdentity()));

        await Assert.ThrowsAsync<RfUnauthorizedException>(() => orders.GetOrderAsync(1));
        Assert.Equal("hi", await orders.EchoAsync("hi"));
    }

    [Fact]
    public async Task Server_mode_decorator_enforces_roles()
    {
        await Assert.ThrowsAsync<RfForbiddenException>(() => ServerModeOrders(User("Viewer")).SaveOrderAsync(new OrderDto(1, "x", 1m, null)));
        await ServerModeOrders(User("Admin")).SaveOrderAsync(new OrderDto(2, "y", 1m, null));
    }

    [Fact]
    public async Task Server_mode_passes_original_exceptions_through()
    {
        var orders = ServerModeOrders(User());

        var expected = await Assert.ThrowsAsync<RfException>(() => orders.FailExpectedAsync("conflict"));
        Assert.Equal("conflict", expected.Code);
        await Assert.ThrowsAsync<RfNotFoundException>(() => orders.FailNotFoundAsync("order_not_found"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => orders.FailUnexpectedAsync());
    }

    [Fact]
    public void Decorating_an_unregistered_interface_throws()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOrderService, OrderService>();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddRForgeAutoApiServer());
        Assert.Contains(nameof(IReportService), ex.Message);
    }

    [Fact]
    public void Decorating_a_singleton_with_authorization_throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();

        Assert.Throws<InvalidOperationException>(() => services.AddRForgeAutoApiServer());
    }

    private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
