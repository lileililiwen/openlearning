using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OpenLearning.Web;
using OpenLearning.Web.Pages;
using OpenLearning.Web.Pages.Courses;
using OpenLearning.Web.Pages.Courses.Lessons;
using Xunit;

namespace OpenLearning.UnitTests.Localization;

/// <summary>
/// Verifies the localization foundation: the en/zh resource strings resolve from the
/// embedded .resx files, an untranslated key falls back to English, and the request
/// localization options are configured with en as default plus zh and the three providers.
/// </summary>
public sealed class LocalizationFoundationTests
{
    private static IStringLocalizer<T> BuildLocalizer<T>() where T : class
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalizationFoundation();
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<T>>();
    }

    private static void WithCulture(string culture, Action body)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            body();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void DefaultCulture_ReturnsEnglishKey_ForMyCourses()
    {
        var localizer = BuildLocalizer<MyCoursesModel>();

        WithCulture("en", () =>
        {
            Assert.Equal("My Courses", localizer["My Courses"].Value);
            Assert.Equal("Continue", localizer["Continue"].Value);
        });
    }

    [Fact]
    public void ZhCulture_ReturnsChinese_ForMyCourses()
    {
        var localizer = BuildLocalizer<MyCoursesModel>();

        WithCulture("zh", () =>
        {
            Assert.Equal("我的课程", localizer["My Courses"].Value);
            Assert.Equal("继续", localizer["Continue"].Value);
        });
    }

    [Fact]
    public void ZhCulture_ReturnsChinese_ForLessonView()
    {
        var localizer = BuildLocalizer<ViewModel>();

        WithCulture("zh", () =>
        {
            Assert.Equal("课程目录", localizer["Catalog"].Value);
            Assert.Equal("返回课程", localizer["Back to course"].Value);
        });
    }

    [Fact]
    public void ZhCulture_ReturnsChinese_ForCourseDetails()
    {
        var localizer = BuildLocalizer<DetailsModel>();

        WithCulture("zh", () =>
        {
            Assert.Equal("编辑课程", localizer["Edit course"].Value);
            Assert.Equal("退课", localizer["Withdraw"].Value);
        });
    }

    [Fact]
    public void ZhCulture_ReturnsChinese_ForCourseEdit()
    {
        var localizer = BuildLocalizer<OpenLearning.Web.Pages.Courses.EditModel>();

        WithCulture("zh", () =>
        {
            Assert.Equal("成果与掌握度", localizer["Outcomes & mastery"].Value);
            Assert.Equal("版本管理", localizer["Version management"].Value);
        });
    }

    [Fact]
    public void UntranslatedKey_FallsBackToEnglish()
    {
        var localizer = BuildLocalizer<MyCoursesModel>();
        const string untranslated = "ThisKeyIsNotInAnyResourceFile";

        WithCulture("zh", () =>
        {
            // No zh (or en) translation exists, so the key itself is returned — visible
            // during development and never throws.
            Assert.Equal(untranslated, localizer[untranslated].Value);
            Assert.True(localizer[untranslated].ResourceNotFound);
        });
    }

    [Fact]
    public void LocalizationFoundation_RegistersRequestLocalizationOptions()
    {
        // AddLocalizationFoundation configures en as the default culture with zh supported
        // and the cookie/query/accept-language providers. The options type lives in the
        // ASP.NET shared framework, which the test project references only transitively, so
        // we resolve it from the service descriptor that AddLocalizationFoundation registered
        // (the type is already loaded) and read its properties reflectively.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalizationFoundation();
        var provider = services.BuildServiceProvider();

        var optionsType = services
            .Select(d => d.ServiceType)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IConfigureOptions<>))
            .Select(t => t.GetGenericArguments()[0])
            .FirstOrDefault(t => t.Name == "RequestLocalizationOptions")
            ?? throw new InvalidOperationException("RequestLocalizationOptions was not registered.");

        var iOptionsType = typeof(IOptions<>).MakeGenericType(optionsType);
        var options = iOptionsType.GetProperty("Value")!.GetValue(provider.GetRequiredService(iOptionsType))!;

        var defaultRequestCulture = optionsType.GetProperty("DefaultRequestCulture")!.GetValue(options)!;
        var defaultCulture = (CultureInfo)defaultRequestCulture.GetType().GetProperty("Culture")!.GetValue(defaultRequestCulture)!;
        var supported = (System.Collections.Generic.IEnumerable<CultureInfo>)optionsType.GetProperty("SupportedCultures")!.GetValue(options)!;
        var providers = (System.Collections.IList)optionsType.GetProperty("RequestCultureProviders")!.GetValue(options)!;

        Assert.Equal("en", defaultCulture.Name);
        Assert.Contains(supported, c => c.Name == "en");
        Assert.Contains(supported, c => c.Name == "zh");
        Assert.Equal(3, providers.Count);
    }
}
