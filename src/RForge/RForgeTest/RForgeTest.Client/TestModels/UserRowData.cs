namespace RForgeTest.Client.TestModels;

public class UserRowData 
{

    public int Id { get; set; } 
    public string FirstName { get; set; }   
    public string LastName { get; set; }
    public string Email { get; set; }
    public List<UserRowData> Children { get; set; } = new();
    public UserRowData Parent { get; set; }
}
