using System.Net.Http.Json;
using TodoApp.Models;
using FluentAssertions;
using System.Text.Json;
using TodoApp.Dtos;
using TodoApp.Pagination;

public class TodoTaskApiTests : IClassFixture<TestingWebApiFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;
    private readonly Random rand = new();

    public TodoTaskApiTests(TestingWebApiFactory<Program> factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetTasksEmptyDatabase()
    {
        var response = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>("/tasks");
        PaginatedResponse<TodoTaskDto> expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto>(), 1, 10, 0);
        response.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetNonExistingTask()
    {
        var response = await _client.GetAsync($"/tasks/{Guid.NewGuid().ToString()}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOneAndGetIt()
    {
        var createdResponseDto = await CreateRandomTodoTask();

        var responseDto = await _client.GetFromJsonAsync<TodoTaskDto>($"/tasks/{createdResponseDto!.Id}");
        responseDto.Should().BeEquivalentTo(createdResponseDto);
    }

    [Fact]
    public async Task CreateOneWithSubSubTaskAndGetTasks()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task2 = await CreateRandomTodoTask("Summary1.1", task1);
        var task3 = await CreateRandomTodoTask("Summary1.1.1", task2);

        task1.SubTaskCount = 1;
        task2.SubTaskCount = 1;

        var responseDto = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>("/tasks");
        responseDto!.TotalItemCount.Should().Be(1);
        responseDto.Data.ToList()[0].Should().BeEquivalentTo(task1);
    }

    [Fact]
    public async Task CreateOneWithSubSubTaskAndGetTheFirstSubTask()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task2 = await CreateRandomTodoTask("Summary1.1", task1);
        var task3 = await CreateRandomTodoTask("Summary1.1.1", task2);

        task1.SubTaskCount = 1;
        task2.SubTaskCount = 1;

        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto>() { task3! }, 1, 10, 1);
        var response = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>($"/tasks/{task2.Id}/subtasks");
        response.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateDeleteAndGetTasks()
    {
        var createdResponseDto = await CreateRandomTodoTask();

        var deleteResponse = await _client.DeleteAsync($"/tasks/{createdResponseDto!.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        PaginatedResponse<TodoTaskDto> expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto>(), 1, 10, 0);
        var response = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>("/tasks");
        response.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateTaskWithSubSubTaskAndMoveSubTask()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task2 = await CreateRandomTodoTask("Summary1.1", task1);
        var task3 = await CreateRandomTodoTask("Summary1.1.1", task2);

        var updateTask2Dto = new UpdateTodoTaskDto(
            "Summary2",
            task2.Description,
            task2.DueDate,
            task2.Priority,
            task2.Status,
            null // Make task2 a top level task
        );

        var updateResponse = await _client.PutAsJsonAsync<UpdateTodoTaskDto>($"/tasks/{task2.Id}", updateTask2Dto);
        updateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        task1.SubTaskCount = 0;
        task2.ParentId = null;
        task2.Summary = "Summary2";
        task2.SubTaskCount = 1;
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto>() { task1, task2 }, 1, 10, 2);
        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>("/tasks");
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestTasksPagination()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task2 = await CreateRandomTodoTask("Summary2");
        var task3 = await CreateRandomTodoTask("Summary3");
        var task4 = await CreateRandomTodoTask("Summary4");
        var task5 = await CreateRandomTodoTask("Summary5");

        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto>() { task3, task4 }, 2, 2, 5);
        expectedResponse.NextPage = new Uri("http://localhost/tasks?pagenumber=3&pagesize=2");
        expectedResponse.PreviousPage = new Uri("http://localhost/tasks?pagenumber=1&pagesize=2");
        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>("/tasks?pagenumber=2&pagesize=2");
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    private async Task<TodoTaskDto> CreateRandomTodoTask(string? summary = null, TodoTaskDto? parent = null)
    {
        {
            var statuses = Enum.GetValues<TodoTaskStatus>();
            var newTask = new CreateTodoTaskDto(
                summary: summary ?? Guid.NewGuid().ToString(),
                description: Guid.NewGuid().ToString(),
                dueDate: DateTimeOffset.UtcNow,
                priority: rand.Next(5),
                status: (TodoTaskStatus)rand.Next(statuses.Length),
                parentId: parent?.Id
            );
            if (parent != null)
            {
                parent.SubTaskCount += 1;
            }

            return await createTodoTaskAsync(newTask);
        }
    }

    private async Task<TodoTaskDto> createTodoTaskAsync(CreateTodoTaskDto createDto, Boolean ensureSuccess = true)
    {
        var response = await _client.PostAsJsonAsync("/tasks", createDto);
        response.EnsureSuccessStatusCode();
        string responseStr = await response.Content.ReadAsStringAsync();
        var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<TodoTaskDto>(responseStr, serializerOptions)!;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return _resetDatabase();
    }
}