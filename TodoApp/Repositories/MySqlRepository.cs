using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Repositories;

public class MySqlRepository : IRepository
{

    private readonly TodoContext _context;
    private readonly ILogger<MySqlRepository> _logger;

    public MySqlRepository(TodoContext context, ILogger<MySqlRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateTodoTaskAsync(TodoTask obj)
    {
        await _context.TodoTask.AddAsync(obj);
        await _context.SaveChangesAsync();
    }

    public async Task<(IReadOnlyCollection<TodoTask> tasks, int count)> GetTodoTasksAsync(
        PaginationFilter paginationFilter, Expression<Func<TodoTask, bool>>? filter = null)
    {
        IQueryable<TodoTask> query = CreateTodoTaskQuery(filter);
        var todoTasks = await query.Skip((paginationFilter.PageNumber - 1) * paginationFilter.PageSize)
                                   .Take(paginationFilter.PageSize)
                                   .ToListAsync();
        var count = await query.CountAsync();
        await PopulateHasSubTaskForSubTasks(todoTasks);

        return (todoTasks, count);
    }

    private IQueryable<TodoTask> CreateTodoTaskQuery(Expression<Func<TodoTask, bool>>? filter = null)
    {
        IQueryable<TodoTask> query = _context.TodoTask.Include(t => t.SubTasks);
        if (filter != null)
            query = query.Where(filter);
        return query;
    }

    public async Task<TodoTask?> GetTodoTaskAsync(Guid id)
    {

        var task = await CreateTodoTaskQuery(t => t.Id == id).FirstOrDefaultAsync();
        await PopulateHasSubTaskForSubTasks(task);
        return task;
    }

    public void DeleteTodoTask(TodoTask obj)
    {
        // TODO: Another way to do this: _context.TodoTask.Where(t => t.Id == obj.Id).ExecuteDeleteAsync();
        _context.TodoTask.Remove(obj);
        _context.SaveChanges();
    }

    public void UpdateTodoTask(TodoTask obj)
    {
        _context.Update<TodoTask>(obj);
        _context.SaveChanges();
    }

    private async Task<bool> TaskHasSubTasks(Guid id)
    {
        return await _context.TodoTask.AnyAsync(t => t.ParentId == id);
    }

    private async Task PopulateHasSubTaskForSubTasks(TodoTask? todoTask)
    {
        if (todoTask == null) return;
        await PopulateHasSubTaskForSubTasks(new List<TodoTask>() { todoTask });
    }

    private async Task PopulateHasSubTaskForSubTasks(ICollection<TodoTask> todoTasks)
    {
        Dictionary<Guid, TodoTask> taskDict = todoTasks.SelectMany(t => t.SubTasks)
                                                       .ToDictionary(t => t.Id, t => t);
        List<Guid> parentIds = taskDict.Keys.ToList();
        var taskIdsWithSubtasks = await _context.TodoTask
            .Where(t => t.ParentId.HasValue && parentIds.Contains(t.ParentId.Value))
            .Select(t => t.ParentId)
            .Distinct()
            .ToListAsync();

        foreach (var taskId in taskIdsWithSubtasks)
        {
            taskDict[taskId!.Value].HasSubTasks = true;
        }
    }
}