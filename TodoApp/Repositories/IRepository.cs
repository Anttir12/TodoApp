using System.Linq.Expressions;
using TodoApp.Models;

namespace TodoApp.Repositories;
public interface IRepository
{
    Task<TodoTask?> GetTodoTaskAsync(Guid id);

    Task CreateTodoTaskAsync(TodoTask obj);

    Task<(IReadOnlyCollection<TodoTask> tasks, int count)> GetTodoTasksAsync(PaginationFilter paginationFilter, Expression<Func<TodoTask, bool>>? filter = null);

    void DeleteTodoTask(TodoTask obj);

    void UpdateTodoTask(TodoTask obj);

    Task<ulong> GetMaxPositionAsync(Guid? parentId = null);

    Task UpdateTaskPositionAsync(TodoTask obj, int newIndex);

    Task RebalancePositionAsync(Guid? parentId);

}