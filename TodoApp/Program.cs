using dotenv.net;
using dotenv.net.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using TodoApp;
using TodoApp.Repositories;
using TodoApp.Services;

DotEnv.Load();
var dotenv = DotEnv.Read();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
builder.Services.AddDbContext<TodoContext>(opt => opt.UseMySQL(
    builder.Configuration.GetConnectionString("MySqlDatabase") ??
    throw new ArgumentNullException("Missing MySqlDatabase ConnectionString")
));

builder.Services.AddScoped<IRepository, MySqlRepository>();
builder.Services.AddScoped<ITodoTaskService, TodoTaskService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "static")),
    RequestPath = "",
    EnableDefaultFiles = true
});

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<CustomExceptionHandlingMiddleware>();

bool applyMigrations = false;
EnvReader.TryGetBooleanValue("migrate", out applyMigrations);
if (applyMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<TodoContext>();
        Console.Write("ConnectionString: " + builder.Configuration.GetConnectionString("MySqlDatabase"));
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
}

app.Run();

public partial class Program { }