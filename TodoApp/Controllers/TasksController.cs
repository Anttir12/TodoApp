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

    /// <response code="204">Task was successfully created</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoTaskDto>> CreateTodoTaskAsync(CreateTodoTaskDto createTodoTaskDto)
    {
        var createdTask = await _tasksService.CreateTodoTaskAsync(createTodoTaskDto);
        return CreatedAtAction(nameof(GetTodoTaskAsync), new { Id = createdTask.Id }, createdTask);
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TodoTaskDto>>> GetTodoTasksAsync([FromQuery] PaginationFilter filter)
    {
        var tasks = await _tasksService.GetTodoTasksAsync(filter, t => t.ParentId == null);
        tasks.CreateHelperURLs(GetCurrentUri());
        return tasks;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TodoTaskDto>>> GetTodoTaskSubTasksAsync(Guid id, [FromQuery] PaginationFilter filter)
    {
        var tasks = await _tasksService.GetTodoTasksAsync(filter, (t => t.ParentId == id));
        tasks.CreateHelperURLs(GetCurrentUri());
        return tasks;
    }

    /// <response code="204">Task was successfully deleted</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <response code="204">Task was successfully updated</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodoTaskAsync(Guid id, UpdateTodoTaskDto updateTodoTask)
    {
        var updated = await _tasksService.UpdateTodoTaskAsync(id, updateTodoTask);
        if (!updated)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <response code="204">Task was successfully updated</response>
    [HttpPut("{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveTodoTaskAsync(Guid id, MoveTodoTaskDto moveDto)
    {
        bool found = await _tasksService.UpdateTaskPositionAsync(id, moveDto.newIndex);
        if (!found)
        {
            return NotFound();
        }
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