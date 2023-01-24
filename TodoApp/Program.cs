using Microsoft.EntityFrameworkCore;
using TodoApp;
using TodoApp.Repositories;

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
// TODO: Check if this could be something else
builder.Services.AddScoped<IRepository, MySqlRepository>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }