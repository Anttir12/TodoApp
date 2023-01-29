using System.Linq.Expressions;
using TodoApp.Dtos;
using TodoApp.Exceptions;
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
                throw new InvalidForeignKeyException("Parent task not found");
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
        if (existingItem.ParentId != newParentId)
        {
            if (newParentId.HasValue)
            {
                var newParent = await GetTodoTaskAsync(newParentId.Value);
                if (newParent == null)
                {
                    throw new InvalidForeignKeyException("Parent task not found");
                }
                else if (newParent.ParentId == existingItem.Id)
                {
                    throw new InvalidForeignKeyException("Updating ParentID would cause circular relationship!");
                }
            }
            newPosition = await GetNextPositionAsync(newParentId);
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

    public async Task<bool> UpdateTaskPositionAsync(Guid taskId, int newIndex)
    {
        var task = await _repository.GetTodoTaskAsync(taskId);
        if (task == null)
        {
            return false;
        }
        await _repository.UpdateTaskPositionAsync(task, newIndex);
        return true;
    }

    private async Task<ulong> GetNextPositionAsync(Guid? parentId)
    {
        var currentMaxPosition = await _repository.GetMaxPositionAsync(parentId);
        if (currentMaxPosition >= (ulong.MaxValue - uint.MaxValue))
        {
            throw new TooManyTasksException("Yo dude that is waaay to many tasks. I don't even bother supporting that");
        }
        return currentMaxPosition + uint.MaxValue;
    }
}