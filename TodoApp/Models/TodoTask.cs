using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Models;

public class TodoTask
{
    public Guid Id { get; set; }

    [Required]
    public string Summary { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTimeOffset CreateDate { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    [Required]
    public int Priority { get; set; }

    [Required]
    public TodoTaskStatus Status { get; set; }

    [Required]
    public ulong Position { get; set; }

    public Guid? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public ICollection<TodoTask> SubTasks { get; set; } = new List<TodoTask>();

    [NotMapped]
    public int SubTaskCount { get; set; } = 0;

    public TodoTask(Guid id, string summary, string? description, DateTimeOffset createDate, DateTimeOffset? dueDate, int priority, TodoTaskStatus status, ulong position, Guid? parentId)
    {
        Id = id;
        Summary = summary;
        Description = description;
        CreateDate = createDate;
        DueDate = dueDate;
        Priority = priority;
        Status = status;
        Position = position;
        ParentId = parentId;
    }

}