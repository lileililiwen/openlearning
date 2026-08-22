using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Data;
using OpenLearning.Navigation.Models;
using OpenLearning.Navigation.Services;
using OpenLearning.SystemConfig.Services;
using Xunit;

namespace OpenLearning.UnitTests.Navigation;

public sealed class NavigationTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static MenuService CreateMenuService(ApplicationDbContext db)
    {
        return new MenuService(new SystemConfigService(db));
    }

    [Fact]
    public async Task Student_sees_only_student_groups()
    {
        var db = CreateDb();
        var menus = CreateMenuService(db);

        var tree = await menus.GetTreeAsync(new[] { Roles.Student });

        Assert.NotEmpty(tree.Groups);
        Assert.All(tree.Groups, g => Assert.Contains("student.", g.Key));
        Assert.All(tree.Groups.SelectMany(g => g.Items), i => Assert.Equal(Roles.Student, i.AllowedRoles));
    }

    [Fact]
    public async Task Admin_sees_admin_groups_and_hidden_items_are_filtered()
    {
        var db = CreateDb();
        var menus = CreateMenuService(db);

        var tree = await menus.GetTreeAsync(new[] { Roles.Admin });
        Assert.Contains(tree.Groups, g => g.Key == "admin.home");
        Assert.Contains(tree.Groups.SelectMany(g => g.Items), i => i.Route == "/Admin/Payments");
        Assert.All(tree.Groups.SelectMany(g => g.Items), i => Assert.Equal(Roles.Admin, i.AllowedRoles));
    }

    [Fact]
    public async Task Finance_sees_payment_gateway_console()
    {
        var db = CreateDb();
        var menus = CreateMenuService(db);

        var tree = await menus.GetTreeAsync(new[] { Roles.Finance });

        var item = Assert.Single(tree.Groups.SelectMany(g => g.Items), i => i.Key == "finance-payments");
        Assert.Equal("/Admin/Payments", item.Route);
        Assert.Equal(Roles.Finance, item.AllowedRoles);
    }

    [Fact]
    public async Task Save_then_load_returns_edited_tree()
    {
        var db = CreateDb();
        var menus = CreateMenuService(db);

        var edited = new MenuTree();
        edited.Groups.Add(new MenuGroup
        {
            Key = "custom",
            Label = "自定义",
            SortOrder = 1,
            Items =
            {
                new MenuItem { Key = "custom-item", GroupKey = "custom", Label = "自定义项", Route = "/MyCourses", SortOrder = 1, AllowedRoles = Roles.Student },
            },
        });

        await menus.SaveTreeAsync(edited);

        var reloaded = await menus.GetFullTreeAsync();
        Assert.Single(reloaded.Groups);
        Assert.Equal("自定义", reloaded.Groups[0].Label);
        Assert.Single(reloaded.Groups[0].Items);

        // A student now sees the custom tree, not the defaults.
        var studentTree = await menus.GetTreeAsync(new[] { Roles.Student });
        Assert.Contains(studentTree.Groups, g => g.Key == "custom");
        Assert.DoesNotContain(studentTree.Groups, g => g.Key == "student.learning");
    }
}
