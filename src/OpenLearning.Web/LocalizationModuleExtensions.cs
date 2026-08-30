using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace OpenLearning.Web;

/// <summary>
/// Wires the ASP.NET Core localization foundation: resource location, the en/zh
/// supported cultures, and the request-culture providers (cookie, query string,
/// accept-language) used to pick the active culture per request.
/// </summary>
public static class LocalizationModuleExtensions
{
    public static IServiceCollection AddLocalizationFoundation(this IServiceCollection services)
    {
        var supportedCultures = new List<CultureInfo>
        {
            new CultureInfo("en"),
            new CultureInfo("zh"),
        };

        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SetDefaultCulture("en");
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new CookieRequestCultureProvider(),
                new QueryStringRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider(),
            };
        });

        return services;
    }
}
