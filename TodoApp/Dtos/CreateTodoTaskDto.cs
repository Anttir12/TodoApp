using TodoApp.Models;

namespace TodoApp.Dtos;

public class CreateTodoTaskDto
{
    public string Summary { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int Priority { get; set; }
    public TodoTaskStatus Status { get; set; }
    public Guid? ParentId { get; set; }

    public CreateTodoTaskDto(string summary, string? description, DateTimeOffset? dueDate, int priority, TodoTaskStatus status, Guid? parentId)
    {
        Summary = summary;
        Description = description;
        DueDate = dueDate;
        Priority = priority;
        Status = status;
        ParentId = parentId;
    }
}