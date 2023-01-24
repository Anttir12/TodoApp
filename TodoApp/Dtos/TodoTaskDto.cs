using TodoApp.Models;
using TodoApp.Pagination;

namespace TodoApp.Dtos;

public class TodoTaskDto
{
    public Guid Id { get; set; }
    public string Summary { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int Priority { get; set; }
    public TodoTaskStatus Status { get; set; }
    public Guid? ParentId { get; set; }
    public ICollection<TodoTaskDto> SubTasks { get; set; } = new List<TodoTaskDto>();
    public bool HasSubTasks { get; set; }

    public TodoTaskDto(Guid id,
                       string summary,
                       string? description,
                       DateTimeOffset createDate,
                       DateTimeOffset? dueDate,
                       int priority,
                       TodoTaskStatus status,
                       Guid? parentId,
                       ICollection<TodoTaskDto> subTasks)
    {
        Id = id;
        Summary = summary;
        Description = description;
        CreateDate = createDate;
        DueDate = dueDate;
        Priority = priority;
        Status = status;
        ParentId = parentId;
        SubTasks = subTasks;
    }
}