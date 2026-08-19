namespace OpenLearning.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";
}

public static class Policies
{
    public const string RequireStudent = "RequireStudent";
    public const string RequireInstructor = "RequireInstructor";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireInstructorOrAdmin = "RequireInstructorOrAdmin";
    public const string AdminMenuConfig = "AdminMenuConfig";
}
