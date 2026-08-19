using System.Collections.Concurrent;
using OpenLearning.Navigation.Models;

namespace OpenLearning.Navigation.Services;

/// <summary>One breadcrumb segment.</summary>
public sealed record BreadcrumbCrumb(string Label, string? Route);

/// <summary>
/// Collects the breadcrumb trail for the current page from
/// <c>[Breadcrumb]</c> attributes on the page-model type (reflection-cached).
/// </summary>
public class BreadcrumbService
{
    private readonly ConcurrentDictionary<Type, IReadOnlyList<BreadcrumbCrumb>> _cache = new();

    public IReadOnlyList<BreadcrumbCrumb> GetCrumbs(Type pageModelType)
    {
        return _cache.GetOrAdd(pageModelType, static type =>
        {
            var attribute = type.GetCustomAttributes(typeof(BreadcrumbAttribute), inherit: true)
                .OfType<BreadcrumbAttribute>()
                .FirstOrDefault();
            if (attribute is null || attribute.Crumbs.Length == 0)
            {
                return Array.Empty<BreadcrumbCrumb>();
            }

            var crumbs = new List<BreadcrumbCrumb>(attribute.Crumbs.Length);
            foreach (var crumb in attribute.Crumbs)
            {
                var separator = crumb.LastIndexOf(":/", StringComparison.Ordinal);
                if (separator > 0)
                {
                    crumbs.Add(new BreadcrumbCrumb(crumb[..separator], crumb[(separator + 1)..]));
                }
                else
                {
                    crumbs.Add(new BreadcrumbCrumb(crumb, null));
                }
            }

            return crumbs;
        });
    }
}
