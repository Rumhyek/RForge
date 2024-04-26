using RForgeDocs.Abstractions.Services;
using RForgeDocs.Client.Implementation;

namespace RForgeDocs.Client;

public class IocConfig
{
    public static void Register(IConfiguration configuration, IServiceCollection services)
    {
        services.AddScoped<IGetUserProcessor, ApiGetUserProcessor>();
        services.AddScoped<ISaveUserProcessor, ApiSaveUserProcessor>();
        services.AddScoped<IFindUsersProcessor, ApiFindUsersProcessor>();
        services.AddScoped<IUserDataGridPageProcessor, ApiUserDataGridPageProcessor>();
    }
}
