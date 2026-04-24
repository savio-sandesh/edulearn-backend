using MassTransit;
using EduLearn.Shared;

namespace EduLearn.Content.API.Consumers;

public class CourseCompletedConsumer : IConsumer<ICourseCompletedEvent>
{
    private readonly ILogger<CourseCompletedConsumer> _logger;

    public CourseCompletedConsumer(ILogger<CourseCompletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ICourseCompletedEvent> context)
    {
        _logger.LogInformation(
            "Event Received: Student {StudentId} completed Course {CourseId}!",
            context.Message.StudentId,
            context.Message.CourseId);

        return Task.CompletedTask;
    }
}
