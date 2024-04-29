using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using RForgeDocsLibrary.Services;

namespace RForgeDocsLibrary.Implementation;
public class UserDataGridPageProcessor : IUserDataGridPageProcessor
{
    private readonly IUserRepository _userRepository;

    public UserDataGridPageProcessor(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GridPageResults<UserData>> GetPage(UserDataGridGetPageData options)
    {
        var query = _userRepository.GetAllUsers();
        var results = new GridPageResults<UserData>();

        //filters
        if (options.Filters.Id.HasValue == true)
            query = query.Where(u => u.Id == options.Filters.Id.Value);
        
        if (options.Filters.IsAdmin.HasValue == true)
            query = query.Where(u => u.IsAdmin == options.Filters.IsAdmin.Value);

        if (options.Filters.DateCreated.HasValue == true)
            query = query.Where(u => u.DateCreated.Date == options.Filters.DateCreated.Value.Date);

        if (string.IsNullOrEmpty(options.Filters.FirstName) == false)
            query = query.Where(u => u.FirstName.Contains(options.Filters.FirstName));

        if (string.IsNullOrEmpty(options.Filters.LastName) == false)
            query = query.Where(u => u.LastName.Contains(options.Filters.LastName));

        if (string.IsNullOrEmpty(options.Filters.Email) == false)
            query = query.Where(u => u.Email.Contains(options.Filters.Email));

        if (string.IsNullOrEmpty(options.Filters.Bio) == false)
            query = query.Where(u => u.Bio.Contains(options.Filters.Bio));

        if (string.IsNullOrEmpty(options.Filters.Username) == false)
            query = query.Where(u => u.Username.Contains(options.Filters.Username));

        if (string.IsNullOrEmpty(options.Filters.FullName) == false)
        {
            var parts = options.Filters.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                query = query.Where(u => u.FirstName.Contains(parts[0]) || u.LastName.Contains(parts[0]));
            }
            else if (parts.Length >= 2)
            {
                query = query
                    .Where(u => (u.FirstName.Contains(parts[0]) == true && u.LastName.Contains(parts[1]) == true)
                             || (u.FirstName.Contains(parts[1]) == true && u.LastName.Contains(parts[0]) == true));
            }
        }

        if (options.SortOrder != RForge.Abstractions.RfSortOrder.None)
        {
            switch (options.SortKey)
            {
                case nameof(UserData.Id):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.Id);
                    else
                        query = query.OrderByDescending(u => u.Id);
                    break;
                case nameof(UserDataGridFilterData.FullName):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query
                            .OrderBy(u => u.LastName)
                            .ThenBy(u => u.FirstName);
                    else
                        query = query
                            .OrderByDescending(u => u.LastName)
                            .OrderByDescending(u => u.FirstName);
                    break;
                case nameof(UserData.FirstName):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.FirstName);
                    else
                        query = query.OrderByDescending(u => u.FirstName);
                    break;
                case nameof(UserData.LastName):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.LastName);
                    else
                        query = query.OrderByDescending(u => u.LastName);
                    break;
                case nameof(UserData.Username):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.Username);
                    else
                        query = query.OrderByDescending(u => u.Username);
                    break;
                case nameof(UserData.Email):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.Email);
                    else
                        query = query.OrderByDescending(u => u.Email);
                    break;
                case nameof(UserData.Bio):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.Bio);
                    else
                        query = query.OrderByDescending(u => u.Bio);
                    break;
                case nameof(UserData.DateCreated):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.DateCreated);
                    else
                        query = query.OrderByDescending(u => u.DateCreated);
                    break;
                case nameof(UserData.IsAdmin):
                    if (options.SortOrder == RForge.Abstractions.RfSortOrder.Ascending)
                        query = query.OrderBy(u => u.IsAdmin);
                    else
                        query = query.OrderByDescending(u => u.IsAdmin);
                    break;
            }
        }

        results.TotalCount = query.Count();

        if (options.PageSize.HasValue == true && options.PageSize.Value > 0)
        {
            query = query.Skip(options.PageSize.Value * options.PageIndex)
                .Take(options.PageSize.Value);
        }

        results.Data.AddRange(query
            .Select(u => new UserData()
            {
                Bio = u.Bio,
                DateCreated = u.DateCreated,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Id = u.Id,
                IsAdmin = u.IsAdmin,
                Username = u.Username
            }));

        return results;
    }
}
