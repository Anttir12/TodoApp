using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Respawn;
using TodoApp;

public class TestingWebApiFactory<TEntryPoint> : WebApplicationFactory<Program>, IAsyncLifetime
{

    private static readonly string _databaseName = "todoapp_test";

    private readonly MySqlTestcontainer _dbContainer = new TestcontainersBuilder<MySqlTestcontainer>()
            .WithDatabase(new MySqlTestcontainerConfiguration
            {
                Database = _databaseName,
                Username = "todouser",
                Password = "todopassword"
            }).Build();

    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;
    public HttpClient HttpClient { get; private set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the currently registered TodoContext and replace with new with different connectionString
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddDbContext<TodoContext>(opt => opt.UseMySQL(_dbContainer.ConnectionString));

            // Ensures the database is created and migrations are ran
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            using (var appContext = scope.ServiceProvider.GetRequiredService<TodoContext>())
            {
                try
                {
                    appContext.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task InitializeAsync()
    {
        InitializeAssertionEquivalanceOptions();
        await _dbContainer.StartAsync();
        _dbConnection = new MySqlConnection(_dbContainer.ConnectionString);
        HttpClient = CreateClient();
        await InitializeRespawner();
    }

    private async Task InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql,
            SchemasToInclude = new[] { _databaseName }
        });
    }

    private void InitializeAssertionEquivalanceOptions()
    {
        AssertionOptions.AssertEquivalencyUsing(options =>
        {
            // Something (Database?) truncates the datetimes.
            options.Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(
                ctx.Expectation, precision: TimeSpan.FromSeconds(1))).WhenTypeIs<DateTimeOffset>();
            options.WithStrictOrdering();
            return options;
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}