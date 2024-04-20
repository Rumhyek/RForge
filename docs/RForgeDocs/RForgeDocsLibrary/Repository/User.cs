using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeDocsLibrary.Repository;
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }

    public string Bio { get; set; }

    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }

    /// <summary>
    /// Not actually used just adding to show differences between <see cref="User"/> and <see cref="RForgeDocs.Abstractions.DataModels.UserData"/>
    /// </summary>
    public string PasswordHash { get; set; }
}
