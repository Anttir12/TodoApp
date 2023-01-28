using Microsoft.AspNetCore.Mvc;
using TodoApp.Dtos;
using TodoApp.Pagination;
using TodoApp.Services;

namespace TodoApp.Controllers;

[ApiController]
[Route("tasks")]
public class TasksController : ControllerBase
{
    private readonly ITodoTaskService _tasksService;

    public TasksController(ITodoTaskService tasksService)
    {
        _tasksService = tasksService;
    }

    [HttpPost]
    public async Task<ActionResult<TodoTaskDto>> CreateTodoTaskAsync(CreateTodoTaskDto createTodoTaskDto)
    {
        var createdTask = await _tasksService.CreateTodoTaskAsync(createTodoTaskDto);
        return CreatedAtAction(nameof(GetTodoTaskAsync), new { Id = createdTask.Id }, createdTask);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TodoTaskDto>>> GetTodoTasksAsync([FromQuery] PaginationFilter filter)
    {
        var tasks = await _tasksService.GetTodoTasksAsync(filter, t => t.ParentId == null);
        tasks.CreateHelperURLs(GetCurrentUri());
        return tasks;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoTaskDto>> GetTodoTaskAsync(Guid id)
    {
        var task = await _tasksService.GetTodoTaskAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        return task;
    }

    [HttpGet("{id:guid}/subtasks")]
    public async Task<ActionResult<PaginatedResponse<TodoTaskDto>>> GetTodoTaskSubTasksAsync(Guid id, [FromQuery] PaginationFilter filter)
    {
        var tasks = await _tasksService.GetTodoTasksAsync(filter, (t => t.ParentId == id));
        tasks.CreateHelperURLs(GetCurrentUri());
        return tasks;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTodoTaskAsync(Guid id)
    {
        bool deleted = await _tasksService.DeleteTodoTaskAsync(id);
        // Only reason for false is if the task is not found
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTodoTaskAsync(Guid id, UpdateTodoTaskDto updateTodoTask)
    {
        var updated = await _tasksService.UpdateTodoTaskAsync(id, updateTodoTask);
        if (!updated)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> MoveTodoTaskAsync(Guid id, MoveTodoTaskDto moveDto)
    {
        await _tasksService.UpdateTaskPositionAsync(id, moveDto.newIndex);
        return NoContent();
    }

    private Uri? GetCurrentUri()
    {
        if (Request == null)
        {
            return null;
        }
        var url = $"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.Path}{Request.QueryString}";
        return new Uri(url);
    }
}