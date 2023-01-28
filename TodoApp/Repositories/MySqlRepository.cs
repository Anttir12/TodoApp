using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
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
                              .AsNoTracking()
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
                query = query.OrderBy(t => t.Priority).ThenBy(t => t.Position);
                break;
            case "priority_desc":
                query = query.OrderByDescending(t => t.Priority).ThenBy(t => t.Position);
                break;
            case "status":
                query = query.OrderBy(t => t.Status).ThenBy(t => t.Position);
                break;
            case "status_desc":
                query = query.OrderByDescending(t => t.Status).ThenBy(t => t.Position);
                break;
            case "summary":
                query = query.OrderBy(t => t.Summary).ThenBy(t => t.Position);
                break;
            case "summary_desc":
                query = query.OrderByDescending(t => t.Summary).ThenBy(t => t.Position);
                break;
            case "description":
                query = query.OrderBy(t => t.Description).ThenBy(t => t.Position);
                break;
            case "description_desc":
                query = query.OrderByDescending(t => t.Description).ThenBy(t => t.Position);
                break;
            case "dueDate":
                query = query.OrderBy(t => t.DueDate).ThenBy(t => t.Position);
                break;
            case "dueDate_desc":
                query = query.OrderByDescending(t => t.DueDate).ThenBy(t => t.Position);
                break;
            case "createDate":
                query = query.OrderBy(t => t.CreateDate).ThenBy(t => t.Position);
                break;
            case "createDate_desc":
                query = query.OrderByDescending(t => t.CreateDate).ThenBy(t => t.Position);
                break;
            case "position_desc":
                query = query.OrderByDescending(t => t.Position);
                break;
            default:
                query = query.OrderBy(t => t.Position);
                break;
        }
        return query;
    }

    private IQueryable<TodoTask> CreateTodoTasksQuery(Expression<Func<TodoTask, bool>>? filter = null)
    {
        IQueryable<TodoTask> query = _context.TodoTask.Include(t => t.SubTasks);
        if (filter != null)
            query = query.Where(filter);
        return query;
    }


    public async Task<TodoTask?> GetTodoTaskAsync(Guid id)
    {

        var task = await CreateTodoTasksQuery(t => t.Id == id).AsNoTracking().FirstOrDefaultAsync();
        if (task != null)
        {
            await PopulateSubTaskCount(task);
        }
        return task;
    }

    public void DeleteTodoTask(TodoTask obj)
    {
        _context.TodoTask.Remove(obj);
        _context.SaveChanges();
    }

    public async Task<ulong> GetMaxPositionAsync(Guid? parentId = null)
    {
        return await _context.TodoTask.Where(t => t.ParentId == parentId).AsNoTracking().MaxAsync(t => (ulong?)t.Position) ?? 0;
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
            .AsNoTracking()
            .ToListAsync();
        foreach (var c in counts)
        {
            taskDict[c.Id!.Value].SubTaskCount = c.Count;
        }
    }

    public async Task RebalancePositionAsync(Guid? parentId)
    {
        await _context.Database.ExecuteSqlRawAsync(@$"
        UPDATE TodoTask t 
            JOIN 
                (SELECT Id, Position, row_number() OVER (ORDER BY Position) as rn
                 FROM TodoTask
                 WHERE ParentId {(parentId == null ? "IS NULL" : $"= @parentId")}
                 ORDER BY Position) as sub 
            ON sub.Id = t.Id
        SET t.Position = sub.rn * {uint.MaxValue};", new MySqlParameter("@parentId", parentId));
    }

    public async Task UpdateTaskPositionAsync(Guid taskId, int newIndex)
    {
        var task = await GetTodoTaskAsync(taskId);
        var currentTaskAtNewIndex = await GetTaskAtIndex(task.ParentId, newIndex);
        // Assume user wants to move task to end of list if nothing found at newIndex;
        ulong positionAtNewIndex = currentTaskAtNewIndex != null ?
            currentTaskAtNewIndex.Position : await GetMaxPositionAsync(task.ParentId);

        // Task is at correct position already
        if (task.Position == positionAtNewIndex)
        {
            return;
        }

        // There is no room before the newIndex. Rebalancing required
        if (positionAtNewIndex <= 1)
        {
            await RebalancePositionAsync(task.ParentId);
            // We should know the position now but to be on the safer side lets just call this again.
            await UpdateTaskPositionAsync(taskId, newIndex);
            return;
        }
        ulong newPosition;
        if (newIndex == 0)
        {
            newPosition = positionAtNewIndex / 2;
        }
        else
        {
            if (task.Position > positionAtNewIndex)
            {
                // We know this should not be null;
                var adjecentTask = await GetTaskAtIndex(task.ParentId, newIndex - 1);
                newPosition = (adjecentTask!.Position + positionAtNewIndex) / 2;
            }
            else
            {
                var adjecentTask = await GetTaskAtIndex(task.ParentId, newIndex + 1);
                newPosition = adjecentTask != null ?
                    (adjecentTask.Position + positionAtNewIndex) / 2 : positionAtNewIndex + uint.MaxValue;
            }
        }
        ulong difference = newPosition > positionAtNewIndex ? newPosition - positionAtNewIndex : positionAtNewIndex - newPosition;

        if (difference <= 1)
        {
            // There was no room between the tasks. Needs rebalancing
            await RebalancePositionAsync(task.ParentId);
            await UpdateTaskPositionAsync(taskId, newIndex);
            return;
        }
        task.Position = newPosition;
        UpdateTodoTask(task);
    }

    private async Task<TodoTask?> GetTaskAtIndex(Guid? parentId, int index)
    {
        return await _context.TodoTask
                     .Where(t => t.ParentId == parentId)
                     .OrderBy(t => t.Position)
                     .Skip(index)
                     .Take(1)
                     .AsNoTracking()
                     .SingleOrDefaultAsync();
    }


}