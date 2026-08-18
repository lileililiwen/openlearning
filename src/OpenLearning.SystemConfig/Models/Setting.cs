namespace OpenLearning.SystemConfig.Models;

/// <summary>A single key-value system parameter editable by admins.</summary>
public class Setting
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
