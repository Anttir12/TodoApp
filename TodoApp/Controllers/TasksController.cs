using Microsoft.AspNetCore.Mvc;
using TodoApp.Dtos;
using TodoApp.Models;
using TodoApp.Pagination;
using TodoApp.Repositories;
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
        TodoTaskDto? parentTask = null;
        if (createTodoTaskDto.ParentId.HasValue)
        {
            parentTask = await _tasksService.GetTodoTaskAsync(createTodoTaskDto.ParentId.Value);
            if (parentTask == null)
            {
                return BadRequest("Invalid parent task"); //TODO Better error messages
            }
        }
        var createdTask = await _tasksService.CreateTodoTaskAsync(createTodoTaskDto);
        return CreatedAtAction(nameof(GetTodoTaskAsync), new { Id = createdTask.Id }, createdTask);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TodoTaskDto>>> GetTodoTasksAsync([FromQuery] PaginationFilter filter)
    {
        var tasks = await _tasksService.GetTodoTasksAsync(filter, t => t.ParentId == null);
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTodoTaskAsync(Guid id)
    {
        bool deleted = await _tasksService.DeleteTodoTask(id);
        // Only reason for false is if the task is not found
        if(!deleted){
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTodoTaskAsync(Guid id, UpdateTodoTaskDto updateTodoTask)
    {
        var updated = await _tasksService.UpdateTodoTask(id, updateTodoTask);
        if (!updated)
        {
            return NotFound();
        }
        return NoContent();
    }
}