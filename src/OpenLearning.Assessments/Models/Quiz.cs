using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Assessments.Models;

public class Quiz
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
