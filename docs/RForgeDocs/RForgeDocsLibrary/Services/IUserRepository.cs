using RForgeDocsLibrary.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeDocsLibrary.Services;
public interface IUserRepository
{
    Task<User> GetUser(int userId);
    Task<bool> HasUserWithUsername(string username);

    IQueryable<User> GetAllUsers();
}
