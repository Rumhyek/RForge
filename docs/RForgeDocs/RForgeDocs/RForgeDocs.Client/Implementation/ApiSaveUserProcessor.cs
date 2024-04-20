using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;

namespace RForgeDocs.Client.Implementation;

public class ApiSaveUserProcessor : ISaveUserProcessor
{
    public Task<int?> AddUser(UserAddSaveData userData)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SaveUser(UserSaveData userData)
    {
        throw new NotImplementedException();
    }
}
