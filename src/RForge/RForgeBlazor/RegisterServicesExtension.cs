using Microsoft.Extensions.DependencyInjection;
using RForge.Abstractions.Modal;
using RForge.Abstractions.Notifications;
using RForgeBlazor.Services;

namespace RForgeBlazor
{
    /// <summary>
    /// Extension methods for registering services in the RForgeBlazor namespace.
    /// </summary>
    public static class RegisterServicesExtension
    {
        /// <summary>
        /// Adds RForgeBlazor services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRfForgeBlazorServices(this IServiceCollection services)
        {

            services.AddScoped<NotificationManager>();
            services.AddScoped<INotificationManager>(x => x.GetRequiredService<NotificationManager>());
            services.AddScoped<INotificationManagerBackend>(x => x.GetRequiredService<NotificationManager>()); 
            
            services.AddScoped<DialogManager>();
            services.AddScoped<IDialogManager>(x => x.GetRequiredService<DialogManager>());
            services.AddScoped<IDialogManagerBackend>(x => x.GetRequiredService<DialogManager>());

            return services;
        }
    }
}
