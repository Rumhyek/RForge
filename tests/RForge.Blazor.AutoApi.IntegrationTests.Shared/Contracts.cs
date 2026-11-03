using System.Text.Json;
using System.Text.Json.Serialization;
using RForge.Blazor.AutoApi;
using RForge.Blazor.AutoApi.IntegrationTests.Shared;

[assembly: RForgeAutoApiDefaults(
    JsonContext = typeof(TestJsonContext),
    ExcludedAttributes = new[] { typeof(DomainOnlyAttribute) })]

namespace RForge.Blazor.AutoApi.IntegrationTests.Shared;

public record OrderDto(int Id, string Name, decimal Total, DateTime? ShippedAt);

public record OrderFilter(string? NameContains, decimal? MinTotal);

public enum OrderStatus { Open, Shipped, Cancelled }

/// <summary>Copied to endpoints as metadata.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
public sealed class EndpointTagAttribute(string value) : Attribute
{
    public string Value { get; } = value;
    public int Weight { get; set; }
}

/// <summary>Listed in ExcludedAttributes, so never copied.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DomainOnlyAttribute : Attribute;

[RForgeAutoApi]
[RfAuthorize]
public interface IOrderService
{
    Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken = default);

    [RfAuthorize(Roles = "Admin")]
    Task SaveOrderAsync(OrderDto order, string? note = null, CancellationToken cancellationToken = default);

    [RfAllowAnonymous]
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Get)]
    Task<string> QueryAsync(string? nameContains, OrderStatus status = OrderStatus.Open, int minTotal = 0, DateOnly? since = null);

    [RfAllowAnonymous]
    Task<string> EchoAsync(string value);

    [RfAllowAnonymous]
    Task<string> EchoAsync(string value, int times);

    [RfAllowAnonymous]
    ValueTask<List<OrderDto>> SearchAsync(OrderFilter filter, int page);

    [RfAllowAnonymous]
    [RForgeAutoApiMethod(HttpMethod = RfHttpMethod.Delete)]
    ValueTask DeleteAsync(Guid id, OrderStatus status);

    [RfAllowAnonymous]
    Task FailExpectedAsync(string code);

    [RfAllowAnonymous]
    Task FailNotFoundAsync(string code);

    [RfAllowAnonymous]
    Task FailUnexpectedAsync();

    [RfAllowAnonymous]
    [RForgeAutoApiMethod(Route = "custom/ping")]
    Task<string> PingAsync();

    [RfAllowAnonymous]
    [EndpointTag("tagged", Weight = 3)]
    [DomainOnly]
    Task<bool> TaggedAsync();
}

[RForgeAutoApi(RouteSegment = "reports", HttpClientName = "ReportsApi")]
public interface IReportService
{
    Task<decimal> TotalAsync();
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(OrderFilter))]
[JsonSerializable(typeof(List<OrderDto>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
public partial class TestJsonContext : JsonSerializerContext;
