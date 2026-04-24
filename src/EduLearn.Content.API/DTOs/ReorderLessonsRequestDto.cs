namespace EduLearn.Content.API.DTOs;

public class ReorderLessonsRequestDto
{
    public IList<int> OrderedLessonIds { get; set; } = new List<int>();
}
