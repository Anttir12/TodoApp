using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp;

public class TodoContext : DbContext
{
    public DbSet<TodoTask> TodoTask { get; set; } = null!;

    public TodoContext(DbContextOptions<TodoContext> options) : base(options)
    { }



}