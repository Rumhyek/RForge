using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RForge.Blazor.Events;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers RForge.Blazor.Events services.</summary>
public static class RfEventServiceCollectionExtensions
{
    /// <summary>
    /// Registers the root <see cref="IRfEventHub"/> as Scoped. Call this in every host that runs interactive
    /// components: the server <c>Program.cs</c> and, for WebAssembly or Auto, the client <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddRForgeEvents(this IServiceCollection services, Action<RfEventOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static d => d.ServiceType == typeof(RfEventOptions)))
        {
            throw new InvalidOperationException("AddRForgeEvents has already been called for this service collection.");
        }

        var options = new RfEventOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddScoped<IRfEventHub>(static sp => new RootEventHub(
            sp.GetRequiredService<RfEventOptions>(),
            sp.GetService<ILoggerFactory>()?.CreateLogger("RForge.Blazor.Events"),
            sp.GetService<PersistentComponentState>()));

        foreach (var registration in options.ServiceRegistrations)
        {
            registration(services);
        }

        return services;
    }
}
