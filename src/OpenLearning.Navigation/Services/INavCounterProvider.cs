namespace OpenLearning.Navigation.Services;

/// <summary>
/// Supplies badge counts for sidebar items. A module registers an
/// implementation (keyed by <see cref="Key"/>) so the navigation shell can
/// surface counts without referencing the owning module.
/// </summary>
public interface INavCounterProvider
{
    /// <summary>Stable key matching a <c>MenuItem.CounterKey</c>.</summary>
    string Key { get; }

    Task<int> GetCountAsync(string userId);
}
