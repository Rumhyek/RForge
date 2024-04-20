using RForgeDocs.Abstractions.DataModels;

namespace RForgeDocs.Abstractions.Services;

public interface ISaveUserProcessor
{
    Task<int?> AddUser(UserAddSaveData userData);
    Task<bool> SaveUser(UserSaveData userData);
}
