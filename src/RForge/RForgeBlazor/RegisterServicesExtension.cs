using Microsoft.Extensions.DependencyInjection;
using RForgeBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor
{
    public static class RegisterServicesExtension
    {
        public static IServiceCollection AddRfForgeBlazorServices(this IServiceCollection services)
        {

            services.AddScoped<INotificationManager, NotificationManager>();
            services.AddScoped<IDialogManager, DialogManager>();

            return services;
        }
    }
}
