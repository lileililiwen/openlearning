using System.Text.Json;
using OpenLearning.Navigation.Models;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Navigation.Services;

/// <summary>
/// Server-side menu tree. The tree is built-in defaults merged with an
/// operator-edited copy stored as JSON under the <c>navigation.menu.v1</c>
/// system-config key; it is filtered to the caller's roles on every request.
/// </summary>
public class MenuService
{
    public const string MenuConfigKey = "navigation.menu.v1";

    private readonly SystemConfigService _config;

    public MenuService(SystemConfigService config)
    {
        _config = config;
    }

    /// <summary>Builds the role-filtered menu tree for the given roles.</summary>
    public async Task<MenuTree> GetTreeAsync(IReadOnlyCollection<string> roles)
    {
        return Filter(await GetFullTreeAsync(), roles);
    }

    /// <summary>The complete tree (unfiltered) for operators editing the menu.</summary>
    public async Task<MenuTree> GetFullTreeAsync()
    {
        var stored = await _config.GetAsync(MenuConfigKey);
        return stored is null ? BuildDefaultTree() : Deserialize(stored);
    }

    /// <summary>Persists an operator-edited menu tree.</summary>
    public async Task SaveTreeAsync(MenuTree tree)
    {
        var json = JsonSerializer.Serialize(tree, _jsonOptions);
        await _config.SetAsync(MenuConfigKey, json);
    }

    public static MenuTree BuildDefaultTree()
    {
        return DefaultMenus.Build();
    }

    private static MenuTree Filter(MenuTree tree, IReadOnlyCollection<string> roles)
    {
        var filtered = new MenuTree();
        foreach (var group in tree.Groups
                     .Where(g => IsAllowed(g.AllowedRoles, roles))
                     .OrderBy(g => g.SortOrder))
        {
            var copy = new MenuGroup
            {
                Key = group.Key,
                Label = group.Label,
                SortOrder = group.SortOrder,
                IconKey = group.IconKey,
                AllowedRoles = group.AllowedRoles,
            };
            copy.Items.AddRange(group.Items
                .Where(i => !i.Hidden && IsAllowed(i.AllowedRoles, roles))
                .OrderBy(i => i.SortOrder));
            if (copy.Items.Count > 0)
            {
                filtered.Groups.Add(copy);
            }
        }

        return filtered;
    }

    /// <summary>Empty role list means "all signed-in roles".</summary>
    private static bool IsAllowed(string allowedRoles, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(allowedRoles))
        {
            return true;
        }

        var allowed = allowedRoles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return allowed.Any(roles.Contains);
    }

    private static MenuTree Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MenuTree>(json, _jsonOptions) ?? BuildDefaultTree();
        }
        catch (JsonException)
        {
            return BuildDefaultTree();
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
