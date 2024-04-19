namespace RForgeDocs.Abstractions.DataModels;

public class UserSaveData
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }

    public string Bio { get; set; }
}
