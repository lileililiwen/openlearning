using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace OpenLearning.Navigation.Services;

/// <summary>Aggregates counts from every registered <see cref="INavCounterProvider"/>.</summary>
public class NavCounterService
{
    private readonly IEnumerable<INavCounterProvider> _providers;
    private readonly IHttpContextAccessor _http;

    public NavCounterService(IEnumerable<INavCounterProvider> providers, IHttpContextAccessor http)
    {
        _providers = providers;
        _http = http;
    }

    /// <summary>Returns the (key → count) map for the current user; empty when signed out.</summary>
    public async Task<IReadOnlyDictionary<string, int>> GetCountsAsync()
    {
        var userId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return new Dictionary<string, int>();
        }

        var result = new Dictionary<string, int>();
        foreach (var provider in _providers)
        {
            var count = await provider.GetCountAsync(userId);
            if (count > 0)
            {
                result[provider.Key] = count;
            }
        }

        return result;
    }
}
