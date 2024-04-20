using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using RForgeDocsLibrary.Services;

namespace RForgeDocsLibrary.Implementation;

public class GetUserProcessor : IGetUserProcessor
{
    private readonly IUserRepository _userRepository;

    public GetUserProcessor(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserData> GetUser(int userId)
    {
        var user = await _userRepository.GetUser(userId);

        if (user == null) return null;

        return new UserData()
        {
            Username = user.Username,
            LastName = user.LastName,
            FirstName = user.FirstName,
            Bio = user.Bio,
            Email = user.Email,
            Id = user.Id,
            DateCreated = user.DateCreated
        };
    }
}
