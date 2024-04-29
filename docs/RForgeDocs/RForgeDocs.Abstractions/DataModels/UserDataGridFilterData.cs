namespace RForgeDocs.Abstractions.DataModels;

public class UserDataGridFilterData
{
    public int? Id { get; set; }
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }

    public bool? IsAdmin { get; set; }

    public string Bio { get; set; }

    public DateTime? DateCreated { get; set; }
}

