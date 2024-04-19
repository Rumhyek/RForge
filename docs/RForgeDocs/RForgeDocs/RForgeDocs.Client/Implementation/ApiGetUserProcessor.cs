using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;

namespace RForgeDocs.Client.Implementation;

public class ApiGetUserProcessor : IGetUserProcessor
{
    public Task<UserData> GetUser(int userId)
    {
        throw new NotImplementedException();
    }
}
