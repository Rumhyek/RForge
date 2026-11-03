using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RForge.Blazor.Bus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers RForge.Blazor.Bus services.</summary>
public static class BusServiceCollectionExtensions
{
    /// <summary>
    /// Registers the root <see cref="IBus"/> as Scoped. Call this in every host that runs interactive
    /// components: the server <c>Program.cs</c> and, for WebAssembly or Auto, the client <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddBlazorBus(this IServiceCollection services, Action<BusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static d => d.ServiceType == typeof(BusOptions)))
        {
            throw new InvalidOperationException("AddBlazorBus has already been called for this service collection.");
        }

        var options = new BusOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddScoped<IBus>(static sp => new RootBus(
            sp.GetRequiredService<BusOptions>(),
            sp.GetService<ILoggerFactory>()?.CreateLogger("RForge.Blazor.Bus"),
            sp.GetService<PersistentComponentState>()));

        foreach (var registration in options.ServiceRegistrations)
        {
            registration(services);
        }

        return services;
    }
}
