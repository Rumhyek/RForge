using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;

namespace RForgeDocsLibrary.Implementation;

public class SaveUserProcessor : ISaveUserProcessor
{
    // Ids 1..100 belong to the mock repository; added users count up from there.
    private static int _lastAddedUserId = 100;

    /// <summary>
    /// Demo implementation: like <see cref="SaveUser"/> it simulates latency and does not
    /// persist anything (the mock repository is read-only). It returns the id the new user would get.
    /// </summary>
    public async Task<int?> AddUser(UserAddSaveData userData)
    {
        await Task.Delay(Random.Shared.Next(1, 1000));

        return Interlocked.Increment(ref _lastAddedUserId);
    }

    public async Task<bool> SaveUser(UserSaveData userData)
    {
        Random ran =new Random();
        await Task.Delay(ran.Next(1, 1000));

        return ran.Next(1, 10) >= 4;
    }
}
