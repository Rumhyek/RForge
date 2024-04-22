using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using RForgeDocsLibrary.Services;

namespace RForgeDocsLibrary.Implementation
{
    public class FindUsersProcessor : IFindUsersProcessor
    {
        private readonly IUserRepository _userRepository;

        public FindUsersProcessor(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserData>> Find(string searchText, int returnCount = 10)
        {
            Random random = new Random();
            await Task.Delay(random.Next(10, 1000));

            var users = _userRepository.GetAllUsers();

            if (string.IsNullOrWhiteSpace(searchText) == false)
            {
                var parts = searchText.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var part in parts)
                {
                    int idTest;
                    bool testId = int.TryParse(part, out idTest);

                    users = users
                        .Where(u => u.FirstName.Contains(part) == true
                                 || u.LastName.Contains(part) == true
                                 || u.Email.Contains(part) == true
                                 || (testId == true && u.Id == idTest));
                }

            }

            return users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(returnCount)
                .Select(u => new UserData()
                {
                    LastName = u.LastName,
                    Bio = u.Bio,
                    DateCreated = u.DateCreated,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    Id = u.Id,
                    Username = u.Username
                })
                .ToList();
        }
    }
}
