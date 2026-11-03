using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RForge.Blazor.AutoApi.IntegrationTests.Shared;

namespace RForge.Blazor.AutoApi.IntegrationTests;

public sealed class RoundTripTests(ServerFixture fixture) : IClassFixture<ServerFixture>
{
    private IOrderService Orders(string? user = "alice", string? roles = null)
        => fixture.CreateClient(user, roles).GetRequiredService<IOrderService>();

    [Fact]
    public async Task Post_with_simple_parameter_returns_complex_result()
    {
        var order = await Orders().GetOrderAsync(7);

        Assert.Equal(new OrderDto(7, "Order 7", 12.5m, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)), order);
    }

    [Fact]
    public async Task Null_result_round_trips()
    {
        Assert.Null(await Orders().GetOrderAsync(404));
    }

    [Fact]
    public async Task Task_method_sends_complex_body_and_optional_parameter()
    {
        var order = new OrderDto(1, "Widget", 3m, null);

        await Orders(roles: "Admin").SaveOrderAsync(order, "rush");

        lock (OrderService.Saved) Assert.Contains((order, "rush"), OrderService.Saved);
    }

    [Fact]
    public async Task Get_sends_simple_parameters_in_query_string()
    {
        var result = await Orders().QueryAsync("a b&c", OrderStatus.Shipped, 5, new DateOnly(2026, 3, 4));

        Assert.Equal("a b&c|Shipped|5|2026-03-04", result);
    }

    [Fact]
    public async Task Get_omits_null_parameters_and_server_uses_defaults()
    {
        var result = await Orders().QueryAsync(null);

        Assert.Equal("<null>|Open|0|<null>", result);
    }

    [Fact]
    public async Task Overloads_map_to_numbered_routes()
    {
        var orders = Orders();

        Assert.Equal("hi", await orders.EchoAsync("hi"));
        Assert.Equal("hihihi", await orders.EchoAsync("hi", 3));
    }

    [Fact]
    public async Task ValueTask_methods_round_trip()
    {
        var results = await Orders().SearchAsync(new OrderFilter("x", 2m), page: 4);

        Assert.Equal(new OrderDto(4, "x", 2m, null), Assert.Single(results));
        await Orders().DeleteAsync(Guid.NewGuid(), OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Custom_route_is_used()
    {
        Assert.Equal("pong", await Orders().PingAsync());

        var endpoints = fixture.App.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "/api/auto/custom/ping");
    }

    [Fact]
    public async Task Interface_level_client_name_and_route_segment_are_used()
    {
        var reports = fixture.CreateClient().GetRequiredService<IReportService>();

        Assert.Equal(99.95m, await reports.TotalAsync());
    }

    [Fact]
    public void Attributes_are_copied_to_endpoint_metadata_except_excluded_ones()
    {
        var endpoint = fixture.App.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "IOrderService_TaggedAsync");

        var tag = endpoint.Metadata.GetMetadata<EndpointTagAttribute>();
        Assert.NotNull(tag);
        Assert.Equal("tagged", tag.Value);
        Assert.Equal(3, tag.Weight);
        Assert.Null(endpoint.Metadata.GetMetadata<DomainOnlyAttribute>());
    }
}
