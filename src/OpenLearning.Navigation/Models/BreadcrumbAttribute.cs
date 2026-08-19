namespace OpenLearning.Navigation.Models;

/// <summary>
/// Declares the breadcrumb trail for a page model. Each crumb is
/// <c>"Label"</c> (current segment, no link) or <c>"Label:/route"</c>
/// (ancestor link). Example: <c>[Breadcrumb("首页:/", "我的课程:/MyCourses", "课程详情")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BreadcrumbAttribute : Attribute
{
    public string[] Crumbs { get; }

    public BreadcrumbAttribute(params string[] crumbs)
    {
        Crumbs = crumbs;
    }
}
