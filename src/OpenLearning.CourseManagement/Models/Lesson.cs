namespace OpenLearning.CourseManagement.Models;

public class Lesson
{
    public int Id { get; set; }

    public int ModuleId { get; set; }

    public Module? Module { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int OrderIndex { get; set; }
}
