
using System.Linq.Expressions;
using TodoApp.Dtos;
using TodoApp.Models;
using TodoApp.Pagination;

namespace TodoApp.Services;

public interface ITodoTaskService
{
    public Task<TodoTaskDto?> GetTodoTaskAsync(Guid id);
    public Task<PaginatedResponse<TodoTaskDto>> GetTodoTasksAsync(PaginationFilter paginationFilter, Expression<Func<TodoTask, bool>>? filter = null);
    public Task<TodoTaskDto> CreateTodoTaskAsync(CreateTodoTaskDto createDto);
    public Task<bool> DeleteTodoTaskAsync(Guid id);
    public Task<bool> UpdateTodoTaskAsync(Guid id, UpdateTodoTaskDto updateDto);
    public Task UpdateTaskPositionAsync(Guid id, int newIndex);


}