using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Navigation.Models;
using OpenLearning.Navigation.Services;

namespace OpenLearning.Web.Pages.Admin.Menu;

[Authorize(Policy = Policies.AdminMenuConfig)]
public class IndexModel : PageModel
{
    private readonly MenuService _menus;

    public IndexModel(MenuService menus)
    {
        _menus = menus;
    }

    [BindProperty]
    public MenuEditModel Input { get; set; } = new();

    public class MenuEditModel
    {
        public List<GroupInput> Groups { get; set; } = new();
    }

    public class GroupInput
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public string AllowedRoles { get; set; } = string.Empty;

        public List<ItemInput> Items { get; set; } = new();
    }

    public class ItemInput
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Route { get; set; } = string.Empty;

        public string? IconKey { get; set; }

        public int SortOrder { get; set; }

        public bool Hidden { get; set; }

        public string AllowedRoles { get; set; } = string.Empty;

        public string? CounterKey { get; set; }
    }

    public async Task OnGetAsync()
    {
        Input = ToEditModel(await _menus.GetFullTreeAsync());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _menus.SaveTreeAsync(ToTree(Input));
        TempData["Message"] = "导航菜单已保存。";
        return RedirectToPage();
    }

    private static MenuEditModel ToEditModel(MenuTree tree)
    {
        var model = new MenuEditModel();
        foreach (var group in tree.Groups.OrderBy(g => g.SortOrder))
        {
            var groupInput = new GroupInput
            {
                Key = group.Key,
                Label = group.Label,
                SortOrder = group.SortOrder,
                AllowedRoles = group.AllowedRoles,
            };
            groupInput.Items.AddRange(group.Items.OrderBy(i => i.SortOrder).Select(i => new ItemInput
            {
                Key = i.Key,
                Label = i.Label,
                Route = i.Route,
                IconKey = i.IconKey,
                SortOrder = i.SortOrder,
                Hidden = i.Hidden,
                AllowedRoles = i.AllowedRoles,
                CounterKey = i.CounterKey,
            }));
            model.Groups.Add(groupInput);
        }

        return model;
    }

    private static MenuTree ToTree(MenuEditModel model)
    {
        var tree = new MenuTree();
        foreach (var group in model.Groups)
        {
            var menuGroup = new MenuGroup
            {
                Key = group.Key,
                Label = group.Label,
                SortOrder = group.SortOrder,
                AllowedRoles = group.AllowedRoles,
            };
            menuGroup.Items.AddRange(group.Items.Select(i => new MenuItem
            {
                Key = i.Key,
                GroupKey = group.Key,
                Label = i.Label,
                Route = i.Route,
                IconKey = i.IconKey,
                SortOrder = i.SortOrder,
                Hidden = i.Hidden,
                AllowedRoles = i.AllowedRoles,
                CounterKey = i.CounterKey,
            }));
            tree.Groups.Add(menuGroup);
        }

        return tree;
    }
}
