using System.Net.Http.Json;
using TodoApp.Models;
using FluentAssertions;
using System.Text.Json;
using TodoApp.Dtos;

public class TodoTaskApiTests : IClassFixture<TestingWebApiFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public TodoTaskApiTests(TestingWebApiFactory<Program> factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetTasksEmptyDatabase()
    {
        var response = await _client.GetAsync("/tasks");
        var responseStr = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        responseStr.Should().Be("[]");
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
        var dto = new CreateTodoTaskDto("Summary", "Description", DateTimeOffset.Now, 3, TodoTaskStatus.Done, null);
        var createdResponseDto = await createTodoTask(dto);

        var responseDto = await _client.GetFromJsonAsync<TodoTaskDto>($"/tasks/{createdResponseDto!.Id}");
        responseDto.Should().BeEquivalentTo(createdResponseDto);
    }

    [Fact]
    public async Task CreateOneWithSubSubTaskAndGetTasks()
    {
        var task1CreateDto = new CreateTodoTaskDto("Summary1", "Description1", DateTimeOffset.Now, 3, TodoTaskStatus.Done, null);
        var task1 = await createTodoTask(task1CreateDto);
        var task2CreateDto = new CreateTodoTaskDto("Summary1.1", "Description1.1", DateTimeOffset.Now, 2, TodoTaskStatus.Reserved, task1!.Id);
        var task2 = await createTodoTask(task2CreateDto);
        var task3CreateDto = new CreateTodoTaskDto("Summary1.1.1", "Description1.1.1", null, 1, TodoTaskStatus.Reserved, task2!.Id);
        var task3 = await createTodoTask(task3CreateDto);

        task1.SubTasks.Add(task2);
        task1.HasSubTasks = true;
        task2.HasSubTasks = true;
        /* This should only return task1 which contains test1.1 as subtask. 
           Test1.1 does not return subtasks but HasSubTasks should still be true.
           that is also why task3 is not added as a subtask to task2
        */
        var responseDto = await _client.GetFromJsonAsync<List<TodoTaskDto>>("/tasks");
        responseDto!.Count.Should().Be(1);
        responseDto[0].Should().BeEquivalentTo(task1);
    }

    [Fact]
    public async Task CreateDeleteAndGetTasks()
    {
        var dto = new CreateTodoTaskDto("Summary", "Description", DateTimeOffset.Now, 3, TodoTaskStatus.Done, null);
        var createdResponseDto = await createTodoTask(dto);

        var deleteResponse = await _client.DeleteAsync($"/tasks/{createdResponseDto!.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var responseDto = await _client.GetFromJsonAsync<List<TodoTaskDto>>("/tasks");
        responseDto!.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateTaskWithSubSubTaskAndMoveSubTask()
    {
        var task1CreateDto = new CreateTodoTaskDto("Summary1", "Description1", DateTimeOffset.Now, 3, TodoTaskStatus.Done, null);
        var task1 = await createTodoTask(task1CreateDto);
        var task2CreateDto = new CreateTodoTaskDto("Summary1.1", "Description1.1", DateTimeOffset.Now, 2, TodoTaskStatus.Reserved, task1!.Id);
        var task2 = await createTodoTask(task2CreateDto);
        var task3CreateDto = new CreateTodoTaskDto("Summary1.1.1", "Description1.1.1", null, 1, TodoTaskStatus.Reserved, task2!.Id);
        var task3 = await createTodoTask(task3CreateDto);

        var updateTask2Dto = new UpdateTodoTaskDto(
            task2.Summary,
            "Updated Description",
            task2.DueDate,
            task2.Priority,
            task2.Status,
            null // Make task2 a top level task
        );

        var expectedUpdatedTask2 = task2;
        expectedUpdatedTask2.ParentId = null;
        expectedUpdatedTask2.Description = "Updated Description";
        expectedUpdatedTask2.SubTasks.Add(task3!);
        expectedUpdatedTask2.HasSubTasks = true;

        var updateResponse = await _client.PutAsJsonAsync<UpdateTodoTaskDto>($"/tasks/{task2.Id}", updateTask2Dto);
        updateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var tasksResponse = await _client.GetFromJsonAsync<List<TodoTaskDto>>("/tasks");
        tasksResponse!.Count.Should().Be(2); // There are now 2 top level tasks
        // Task1 should no longer have subtasks
        var updatedTask1 = tasksResponse.First(t => t.Id == task1.Id);
        updatedTask1.SubTasks.Should().BeEquivalentTo(new List<TodoTaskDto>());
        // Task2 no longer has parent, Description updated and task3 as subtask
        var updatedTask2 = tasksResponse.First(t => t.Id == task2.Id);
        updatedTask2.Should().BeEquivalentTo(expectedUpdatedTask2);

    }

    private async Task<TodoTaskDto?> createTodoTask(CreateTodoTaskDto createDto, Boolean ensureSuccess = true)
    {
        var response = await _client.PostAsJsonAsync("/tasks", createDto);
        response.EnsureSuccessStatusCode();
        string responseStr = await response.Content.ReadAsStringAsync();
        var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<TodoTaskDto>(responseStr, serializerOptions);
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