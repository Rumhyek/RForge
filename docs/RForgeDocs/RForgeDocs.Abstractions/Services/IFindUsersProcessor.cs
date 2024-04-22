using RForgeDocs.Abstractions.DataModels;

namespace RForgeDocs.Abstractions.Services;

public interface IFindUsersProcessor
{
    Task<List<UserData>> Find(string searchText, int returnCount = 10);
}