using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RForgeDocs.Abstractions.DataModels;

namespace RForgeDocs.Abstractions.Services;
public interface IGetUserProcessor
{
    Task<UserData> GetUser(int userId);
}
