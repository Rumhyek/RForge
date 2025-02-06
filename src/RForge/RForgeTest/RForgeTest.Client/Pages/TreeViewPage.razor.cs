using Microsoft.AspNetCore.Components;
using RForgeTest.Client.TestModels;

namespace RForgeTest.Client.Pages;
public partial class TreeViewPage
{

    [Inject]
    public IGetFakeUsers GetFakeUsers { get; set; }

    public List<UserRowData> RootUsers { get; set; }


    public bool AllowSelection { get; set; } = true;
    public bool AllowExpand { get; set; } = true;

    private void SetupUserTree(List<UserRowData> users)
    {
        
        var rootUsers = new List<UserRowData>();
        var random = new Random();

        foreach (var user in users)
        {
            if (random.Next(5) == 0 || rootUsers.Count == 0)
            {
                rootUsers.Add(user);
            }
            else
            {
                //Add Child User
                var parent = users[random.Next(users.Count)];
                
                parent.Children.Add(user);
                user.Parent = parent;
            }
        }

        RootUsers = rootUsers;
    }

    private void OnLoadUsers()
    {
        var allUsers = GetFakeUsers.Get();
        SetupUserTree(allUsers.ToList());
    }

    private void ClearUsers() => RootUsers = null;
}