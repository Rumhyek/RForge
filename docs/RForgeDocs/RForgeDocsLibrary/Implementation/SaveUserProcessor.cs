using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;

namespace RForgeDocsLibrary.Implementation;

public class SaveUserProcessor : ISaveUserProcessor
{
    public Task<int?> AddUser(UserAddSaveData userData)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SaveUser(UserSaveData userData)
    {
        Random ran =new Random();
        await Task.Delay(ran.Next(1, 1000));

        return ran.Next(1, 10) >= 4;
    }
}
