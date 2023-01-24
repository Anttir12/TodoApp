using TodoApp.Dtos;
using TodoApp.Models;

namespace TodoApp;

public static class Extensions
{
    public static TodoTaskDto asDto(this TodoTask todoTask)
    {
        return new TodoTaskDto(
            todoTask.Id,
            todoTask.Summary,
            todoTask.Description,
            todoTask.CreateDate,
            todoTask.DueDate,
            todoTask.Priority,
            todoTask.Status,
            todoTask.ParentId,
            todoTask.SubTasks.Select(t => t.asDto()).ToList()
            );
    }
}