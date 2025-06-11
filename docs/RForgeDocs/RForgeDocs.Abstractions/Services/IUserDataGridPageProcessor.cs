using RForgeDocs.Abstractions.DataModels;

namespace RForgeDocs.Abstractions.Services;

public interface IUserDataGridPageProcessor
{
    Task<GridPageResults<UserData>> GetPage(UserDataGridGetPageData options);
    Task<List<UserData>> GetAll();
}