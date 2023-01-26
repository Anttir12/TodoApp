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
        IQueryable<TodoTask> query = CreateTodoTasksQuery(filter);
        var todoTasks = await ApplyOrderBy(query, paginationFilter)
                              .Skip((paginationFilter.PageNumber - 1) * paginationFilter.PageSize)
                              .Take(paginationFilter.PageSize)
                              .ToListAsync();
        var count = await query.CountAsync();
        await PopulateSubTaskCounts(todoTasks);
        return (todoTasks, count);
    }

    private IQueryable<TodoTask> ApplyOrderBy(IQueryable<TodoTask> query, PaginationFilter paginationFilter)
    {
        switch (paginationFilter.SortOrder)
        {
            case "priority":
                query = query.OrderBy(t => t.Priority).ThenBy(t => t.CreateDate);
                break;
            case "priority_desc":
                query = query.OrderByDescending(t => t.Priority).ThenBy(t => t.CreateDate);
                break;
            case "status":
                query = query.OrderBy(t => t.Status).ThenBy(t => t.CreateDate);
                break;
            case "status_desc":
                query = query.OrderByDescending(t => t.Status).ThenBy(t => t.CreateDate);
                break;
            case "summary":
                query = query.OrderBy(t => t.Summary).ThenBy(t => t.CreateDate);
                break;
            case "summary_desc":
                query = query.OrderByDescending(t => t.Summary).ThenBy(t => t.CreateDate);
                break;
            case "description":
                query = query.OrderBy(t => t.Description).ThenBy(t => t.CreateDate);
                break;
            case "description_desc":
                query = query.OrderByDescending(t => t.Description).ThenBy(t => t.CreateDate);
                break;
            case "dueDate":
                query = query.OrderBy(t => t.DueDate).ThenBy(t => t.CreateDate);
                break;
            case "dueDate_desc":
                query = query.OrderByDescending(t => t.DueDate).ThenBy(t => t.CreateDate);
                break;
            case "createDate_desc":
                query = query.OrderByDescending(t => t.CreateDate);
                break;
            default:
                query = query.OrderBy(t => t.CreateDate);
                break;
        }
        return query;
    }

    private IQueryable<TodoTask> CreateTodoTasksQuery(Expression<Func<TodoTask, bool>>? filter = null)
    {
        // TODO: For some reason couldn't figure out how to do this to get subtask count in the same query:
        // SELECT t1.*, count(t2.id) as SubTaskCount FROM TodoTask t1 LEFT JOIN TodoTask t2 ON t1.id = t2.ParentId GROUP BY t1.id;
        IQueryable<TodoTask> query = _context.TodoTask.Include(t => t.SubTasks);
        if (filter != null)
            query = query.Where(filter);
        return query;
    }


    public async Task<TodoTask?> GetTodoTaskAsync(Guid id)
    {

        var task = await CreateTodoTasksQuery(t => t.Id == id).FirstOrDefaultAsync();
        if (task != null)
        {
            await PopulateSubTaskCount(task);
        }
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

    private async Task PopulateSubTaskCount(TodoTask todoTask)
    {
        await PopulateSubTaskCounts(new List<TodoTask>() { todoTask });
    }

    private async Task PopulateSubTaskCounts(ICollection<TodoTask> todoTasks)
    {
        Dictionary<Guid, TodoTask> taskDict = todoTasks.ToDictionary(t => t.Id, t => t);
        List<Guid> parentIds = taskDict.Keys.ToList();
        var counts = await _context.TodoTask
            .Where(t => t.ParentId != null)
            .Where(t => parentIds.Contains(t.ParentId!.Value))
            .GroupBy(t => t.ParentId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();
        foreach (var c in counts)
        {
            taskDict[c.Id!.Value].SubTaskCount = c.Count;
        }
    }
}