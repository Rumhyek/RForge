using Microsoft.Extensions.DependencyInjection;
using RForgeBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            services.AddScoped<INotificationManager, NotificationManager>();
            services.AddScoped<IDialogManager, DialogManager>();

            return services;
        }
    }
}
