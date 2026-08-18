namespace OpenLearning.CourseManagement.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1716:IdentifiersShouldNotMatchKeywords",
    Justification = "'Module' is the canonical course-structure domain term (Course/Module/Lesson aggregate); renaming would ripple across schema, services, and pages.")]
public class Module
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string Title { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
