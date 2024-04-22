using RForgeDocs.Abstractions.Services;
using RForgeDocsLibrary.Implementation;
using RForgeDocsLibrary.Services;

namespace RForgeDocs;

public class IocConfig
{
    public static void Register(IConfiguration configuration, IServiceCollection services)
    {

        services.AddScoped<IUserRepository, MockUserRepository>();
        services.AddScoped<IGetUserProcessor, GetUserProcessor>();
        services.AddScoped<ISaveUserProcessor, SaveUserProcessor>();
        services.AddScoped<IFindUsersProcessor, FindUsersProcessor>();
    }
}
