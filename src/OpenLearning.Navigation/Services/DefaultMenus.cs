using OpenLearning.Auth;
using OpenLearning.Navigation.Models;

namespace OpenLearning.Navigation.Services;

/// <summary>The built-in role menus shown when no operator edit has been saved.</summary>
public static class DefaultMenus
{
    public static MenuTree Build()
    {
        var tree = new MenuTree();
        tree.Groups.Add(StudentGroups());
        tree.Groups.Add(StudentCourses());
        tree.Groups.Add(InstructorGroups());
        tree.Groups.Add(InstructorCourses());
        tree.Groups.Add(TaGroups());
        tree.Groups.Add(FinanceGroups());
        tree.Groups.Add(AdminGroups());
        tree.Groups.Add(AdminUsers());
        tree.Groups.Add(AdminCourses());
        tree.Groups.Add(AdminModeration());
        tree.Groups.Add(AdminOps());
        tree.Groups.Add(AdminSystem());
        return tree;
    }

    private static MenuGroup TaGroups()
    {
        var group = new MenuGroup { Key = "ta.workbench", Label = "助教工作台", SortOrder = 25, IconKey = "bi-person-video3", AllowedRoles = Roles.TeachingAssistant };
        group.Items.Add(new MenuItem { Key = "ta-index", GroupKey = group.Key, Label = "我的班级", Route = "/TA/Index", SortOrder = 1, IconKey = "bi-people", AllowedRoles = Roles.TeachingAssistant });
        group.Items.Add(new MenuItem { Key = "ta-reminders", GroupKey = group.Key, Label = "班级提醒", Route = "/TA/Reminders", SortOrder = 2, IconKey = "bi-bell", AllowedRoles = Roles.TeachingAssistant });
        return group;
    }

    private static MenuGroup FinanceGroups()
    {
        var group = new MenuGroup { Key = "finance.workbench", Label = "财务工作台", SortOrder = 45, IconKey = "bi-cash-stack", AllowedRoles = Roles.Finance };
        group.Items.Add(new MenuItem { Key = "finance-orders", GroupKey = group.Key, Label = "订单管理", Route = "/Admin/Orders", SortOrder = 1, IconKey = "bi-receipt", AllowedRoles = Roles.Finance });
        group.Items.Add(new MenuItem { Key = "finance-refunds", GroupKey = group.Key, Label = "退款审核", Route = "/Admin/Refunds", SortOrder = 2, IconKey = "bi-arrow-counterclockwise", AllowedRoles = Roles.Finance });
        group.Items.Add(new MenuItem { Key = "finance-reconciliation", GroupKey = group.Key, Label = "对账报表", Route = "/Admin/Reconciliation", SortOrder = 3, IconKey = "bi-calculator", AllowedRoles = Roles.Finance });
        group.Items.Add(new MenuItem { Key = "finance-withdrawals", GroupKey = group.Key, Label = "提现审核", Route = "/Admin/Withdrawals", SortOrder = 4, IconKey = "bi-cash-coin", AllowedRoles = Roles.Finance });
        group.Items.Add(new MenuItem { Key = "finance-coupons", GroupKey = group.Key, Label = "优惠券", Route = "/Admin/Coupons", SortOrder = 5, IconKey = "bi-ticket-perforated", AllowedRoles = Roles.Finance });
        return group;
    }

    private static MenuGroup StudentGroups()
    {
        var group = new MenuGroup { Key = "student.learning", Label = "学习中心", SortOrder = 10, IconKey = "bi-house", AllowedRoles = Roles.Student };
        group.Items.Add(new MenuItem { Key = "dashboard", GroupKey = group.Key, Label = "学习仪表盘", Route = "/Dashboard/Index", SortOrder = 1, IconKey = "bi-speedometer2", AllowedRoles = Roles.Student });
        group.Items.Add(new MenuItem { Key = "study-plan", GroupKey = group.Key, Label = "学习计划", Route = "/Study", SortOrder = 2, IconKey = "bi-calendar-check", AllowedRoles = Roles.Student, CounterKey = "study.todo" });
        return group;
    }

    private static MenuGroup StudentCourses()
    {
        var group = new MenuGroup { Key = "student.courses", Label = "我的课程", SortOrder = 20, IconKey = "bi-journal-bookmark", AllowedRoles = Roles.Student };
        group.Items.Add(new MenuItem { Key = "my-courses", GroupKey = group.Key, Label = "我的课程", Route = "/MyCourses", SortOrder = 1, IconKey = "bi-collection", AllowedRoles = Roles.Student });
        group.Items.Add(new MenuItem { Key = "catalog", GroupKey = group.Key, Label = "课程目录", Route = "/Index", SortOrder = 2, IconKey = "bi-search", AllowedRoles = Roles.Student });
        return group;
    }

    private static MenuGroup InstructorGroups()
    {
        var group = new MenuGroup { Key = "instructor.workbench", Label = "教师工作台", SortOrder = 10, IconKey = "bi-briefcase", AllowedRoles = Roles.Instructor };
        group.Items.Add(new MenuItem { Key = "teacher-dashboard", GroupKey = group.Key, Label = "教师仪表盘", Route = "/Dashboard/Teacher", SortOrder = 1, IconKey = "bi-speedometer2", AllowedRoles = Roles.Instructor });
        group.Items.Add(new MenuItem { Key = "revenue", GroupKey = group.Key, Label = "收入结算", Route = "/Instructor/Revenue", SortOrder = 2, IconKey = "bi-currency-yen", AllowedRoles = Roles.Instructor });
        return group;
    }

    private static MenuGroup InstructorCourses()
    {
        var group = new MenuGroup { Key = "instructor.courses", Label = "课程管理", SortOrder = 20, IconKey = "bi-journal-bookmark", AllowedRoles = Roles.Instructor };
        group.Items.Add(new MenuItem { Key = "manage-courses", GroupKey = group.Key, Label = "我的课程", Route = "/Courses/Manage", SortOrder = 1, IconKey = "bi-collection", AllowedRoles = Roles.Instructor });
        group.Items.Add(new MenuItem { Key = "new-course", GroupKey = group.Key, Label = "创建课程", Route = "/Courses/Create", SortOrder = 2, IconKey = "bi-plus-circle", AllowedRoles = Roles.Instructor });
        group.Items.Add(new MenuItem { Key = "roster", GroupKey = group.Key, Label = "学员管理", Route = "/Courses/Roster", SortOrder = 3, IconKey = "bi-people", AllowedRoles = Roles.Instructor });
        return group;
    }

    private static MenuGroup AdminGroups()
    {
        var group = new MenuGroup { Key = "admin.home", Label = "后台首页", SortOrder = 10, IconKey = "bi-speedometer2", AllowedRoles = Roles.Admin };
        group.Items.Add(new MenuItem { Key = "admin-dashboard", GroupKey = group.Key, Label = "平台仪表盘", Route = "/Admin/Index", SortOrder = 1, IconKey = "bi-speedometer2", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "reports", GroupKey = group.Key, Label = "数据报表", Route = "/Admin/Reports/Revenue", SortOrder = 2, IconKey = "bi-bar-chart", AllowedRoles = Roles.Admin });
        return group;
    }

    private static MenuGroup AdminUsers()
    {
        var group = new MenuGroup { Key = "admin.users", Label = "用户管理", SortOrder = 20, IconKey = "bi-people", AllowedRoles = Roles.Admin };
        group.Items.Add(new MenuItem { Key = "users", GroupKey = group.Key, Label = "用户列表", Route = "/Admin/Users", SortOrder = 1, IconKey = "bi-people", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "applications", GroupKey = group.Key, Label = "教师申请", Route = "/Admin/InstructorApplications", SortOrder = 2, IconKey = "bi-person-check", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "identities", GroupKey = group.Key, Label = "实名认证", Route = "/Admin/Identities", SortOrder = 3, IconKey = "bi-shield-check", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "plans", GroupKey = group.Key, Label = "会员方案", Route = "/Admin/MembershipPlans", SortOrder = 4, IconKey = "bi-award", AllowedRoles = Roles.Admin });
        return group;
    }

    private static MenuGroup AdminCourses()
    {
        var group = new MenuGroup { Key = "admin.courses", Label = "课程管理", SortOrder = 30, IconKey = "bi-journal-bookmark", AllowedRoles = Roles.Admin };
        group.Items.Add(new MenuItem { Key = "admin-course-list", GroupKey = group.Key, Label = "课程列表", Route = "/Admin/Courses", SortOrder = 1, IconKey = "bi-collection", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "categories", GroupKey = group.Key, Label = "分类管理", Route = "/Admin/Categories", SortOrder = 2, IconKey = "bi-tags", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "tags", GroupKey = group.Key, Label = "标签管理", Route = "/Admin/Tags", SortOrder = 3, IconKey = "bi-tag", AllowedRoles = Roles.Admin });
        return group;
    }

    private static MenuGroup AdminModeration()
    {
        var group = new MenuGroup { Key = "admin.moderation", Label = "内容审核", SortOrder = 35, IconKey = "bi-shield-check", AllowedRoles = Roles.Admin };
        group.Items.Add(new MenuItem { Key = "course-reviews", GroupKey = group.Key, Label = "课程审核", Route = "/Admin/CourseReviews", SortOrder = 1, IconKey = "bi-journal-check", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "content-reports", GroupKey = group.Key, Label = "内容举报", Route = "/Admin/ContentReports", SortOrder = 2, IconKey = "bi-flag", AllowedRoles = Roles.Admin });
        return group;
    }

    private static MenuGroup AdminOps()
    {
        var group = new MenuGroup { Key = "admin.resources", Label = "资源管理", SortOrder = 40, IconKey = "bi-server", AllowedRoles = Roles.Admin };
        group.Items.Add(new MenuItem { Key = "operations", GroupKey = group.Key, Label = "运营配置", Route = "/Admin/Operations", SortOrder = 1, IconKey = "bi-megaphone", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "orders", GroupKey = group.Key, Label = "订单管理", Route = "/Admin/Orders", SortOrder = 2, IconKey = "bi-receipt", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "refunds", GroupKey = group.Key, Label = "退款审核", Route = "/Admin/Refunds", SortOrder = 3, IconKey = "bi-arrow-counterclockwise", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "withdrawals", GroupKey = group.Key, Label = "提现审核", Route = "/Admin/Withdrawals", SortOrder = 4, IconKey = "bi-cash-coin", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "reconciliation", GroupKey = group.Key, Label = "对账报表", Route = "/Admin/Reconciliation", SortOrder = 5, IconKey = "bi-calculator", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "coupons", GroupKey = group.Key, Label = "优惠券", Route = "/Admin/Coupons", SortOrder = 6, IconKey = "bi-ticket-perforated", AllowedRoles = Roles.Admin });
        return group;
    }

    private static MenuGroup AdminSystem()
    {
        var group = new MenuGroup { Key = "admin.system", Label = "系统配置", SortOrder = 50, IconKey = "bi-gear", AllowedRoles = Roles.Admin };
        group.Items.Add(new MenuItem { Key = "system", GroupKey = group.Key, Label = "系统设置", Route = "/Admin/System", SortOrder = 1, IconKey = "bi-gear", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "menu-config", GroupKey = group.Key, Label = "导航菜单", Route = "/Admin/Menu", SortOrder = 2, IconKey = "bi-list-nested", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "admin-jobs", GroupKey = group.Key, Label = "任务调度", Route = "/Admin/Jobs", SortOrder = 3, IconKey = "bi-clock-history", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "ops-logs", GroupKey = group.Key, Label = "操作日志", Route = "/Admin/Logs/Operations", SortOrder = 4, IconKey = "bi-journal-text", AllowedRoles = Roles.Admin });
        group.Items.Add(new MenuItem { Key = "error-logs", GroupKey = group.Key, Label = "错误日志", Route = "/Admin/Logs/Errors", SortOrder = 5, IconKey = "bi-bug", AllowedRoles = Roles.Admin });
        return group;
    }
}
