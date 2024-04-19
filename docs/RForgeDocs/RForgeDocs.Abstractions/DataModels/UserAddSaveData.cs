using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeDocs.Abstractions.DataModels;
public class UserAddSaveData
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }

    public string Bio { get; set; }
}
