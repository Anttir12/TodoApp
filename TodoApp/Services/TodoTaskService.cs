using System.Linq.Expressions;
using TodoApp.Dtos;
using TodoApp.Models;
using TodoApp.Pagination;
using TodoApp.Repositories;

namespace TodoApp.Services;

public class TodoTaskService : ITodoTaskService
{

    private readonly IRepository _repository;

    public TodoTaskService(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoTaskDto> CreateTodoTaskAsync(CreateTodoTaskDto createDto)
    {
        TodoTaskDto? parentTask = null;
        if (createDto.ParentId.HasValue)
        {
            parentTask = await GetTodoTaskAsync(createDto.ParentId.Value);
            if (parentTask == null)
            {
                throw new Exception("Invalid parent task"); //TODO Better error messages
            }
        }

        var task = new TodoTask
        (
            Guid.NewGuid(),
            createDto.Summary,
            createDto.Description,
            DateTimeOffset.UtcNow,
            createDto.DueDate,
            createDto.Priority,
            createDto.Status,
            await GetNextPositionAsync(createDto.ParentId),
            createDto.ParentId
        );
        await _repository.CreateTodoTaskAsync(task);

        return task.asDto();
    }

    public async Task<bool> DeleteTodoTaskAsync(Guid id)
    {
        var todoTask = await _repository.GetTodoTaskAsync(id);
        if (todoTask == null)
        {
            return false;
        }
        _repository.DeleteTodoTask(todoTask);
        return true;
    }

    public async Task<TodoTaskDto?> GetTodoTaskAsync(Guid id)
    {
        var todoTask = await _repository.GetTodoTaskAsync(id);
        return todoTask?.asDto();
    }

    public async Task<PaginatedResponse<TodoTaskDto>> GetTodoTasksAsync(PaginationFilter paginationFilter, Expression<Func<TodoTask, bool>>? filter = null)
    {
        var result = await _repository.GetTodoTasksAsync(paginationFilter, filter);
        var tasks = result.tasks;
        var count = result.count;
        return new PaginatedResponse<TodoTaskDto>(tasks.Select(t => t.asDto()).ToList(),
         paginationFilter.PageNumber, paginationFilter.PageSize, count);

    }

    public async Task<bool> UpdateTodoTaskAsync(Guid id, UpdateTodoTaskDto updateDto)
    {
        var existingItem = await _repository.GetTodoTaskAsync(id);
        if (existingItem == null)
        {
            return false;
        }
        Guid? newParentId = updateDto.ParentId;
        ulong newPosition = existingItem.Position;
        if (existingItem.ParentId != newParentId && newParentId.HasValue)
        {
            var newParent = await GetTodoTaskAsync(newParentId.Value);
            if (newParent != null)
            {
                if (newParent.ParentId == id)
                {
                    // TODO create custom exception + exception handler
                    throw new Exception("Updating ParentID would cause circular relationship!");
                }
                newPosition = await GetNextPositionAsync(newParentId);
            }
            else
            {
                // TODO create custom exception + exception handler
                throw new Exception("Invalid ParentId");
            }
        }

        existingItem.Summary = updateDto.Summary;
        existingItem.Description = updateDto.Description;
        existingItem.DueDate = updateDto.DueDate;
        existingItem.Priority = updateDto.Priority;
        existingItem.Status = updateDto.Status;
        existingItem.Position = newPosition;
        existingItem.ParentId = newParentId;
        _repository.UpdateTodoTask(existingItem);
        return true;
    }

    public async Task UpdateTaskPositionAsync(Guid taskId, int newIndex)
    {
        await _repository.UpdateTaskPositionAsync(taskId, newIndex);
    }

    private async Task<ulong> GetNextPositionAsync(Guid? parentId)
    {
        var currentMaxPosition = await _repository.GetMaxPositionAsync(parentId);
        if (currentMaxPosition >= (ulong.MaxValue - uint.MaxValue))
        {
            throw new Exception("Yo dude that is waaay to many tasks. I don't even bother supporting that");
        }
        return currentMaxPosition + uint.MaxValue;
    }
}