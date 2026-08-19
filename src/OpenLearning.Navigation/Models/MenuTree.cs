namespace OpenLearning.Navigation.Models;

/// <summary>One navigation item inside a menu group.</summary>
public sealed class MenuItem
{
    public string Key { get; set; } = string.Empty;

    public string GroupKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Razor page route, e.g. "/MyCourses".</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Bootstrap icon glyph name (optional).</summary>
    public string? IconKey { get; set; }

    public int SortOrder { get; set; }

    public bool Hidden { get; set; }

    /// <summary>Comma-separated role names allowed to see this item (empty = all signed-in roles).</summary>
    public string AllowedRoles { get; set; } = string.Empty;

    /// <summary>Registry key of an <see cref="OpenLearning.Navigation.Services.INavCounterProvider"/> badge (optional).</summary>
    public string? CounterKey { get; set; }
}

/// <summary>A named group of navigation items.</summary>
public sealed class MenuGroup
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? IconKey { get; set; }

    /// <summary>Comma-separated role names allowed to see this group (empty = all signed-in roles).</summary>
    public string AllowedRoles { get; set; } = string.Empty;

    public List<MenuItem> Items { get; set; } = new();
}

/// <summary>Root menu tree stored as JSON under the <c>navigation.menu.v1</c> system-config key.</summary>
public sealed class MenuTree
{
    public List<MenuGroup> Groups { get; set; } = new();
}
