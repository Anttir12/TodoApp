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
        var task2 = await CreateRandomTodoTask("Summary1.1", parent: task1);
        var task3 = await CreateRandomTodoTask("Summary1.1.1", parent: task2);

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
        var task2 = await CreateRandomTodoTask("Summary1.1", parent: task1);
        var task3 = await CreateRandomTodoTask("Summary1.1.1", parent: task2);

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
        var task2 = await CreateRandomTodoTask("Summary1.1", parent: task1);
        var task3 = await CreateRandomTodoTask("Summary1.1.1", parent: task2);

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
        task2.Position = (ulong)uint.MaxValue * 2;
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

        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>("/tasks?pagenumber=2&pagesize=2");

        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto>() { task3, task4 }, 2, 2, 5);
        expectedResponse.NextPage = new Uri("http://localhost/tasks?pagenumber=3&pagesize=2");
        expectedResponse.PreviousPage = new Uri("http://localhost/tasks?pagenumber=1&pagesize=2");
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    [Theory]
    [InlineData(new int[] { 0, 1, 2, 4, 3 }, "summary")]
    [InlineData(new int[] { 3, 4, 2, 1, 0 }, "summary_desc")]
    [InlineData(new int[] { 4, 0, 1, 2, 3 }, "description")]
    [InlineData(new int[] { 3, 2, 1, 0, 4 }, "description_desc")]
    [InlineData(new int[] { 3, 4, 0, 1, 2 }, "dueDate")]
    [InlineData(new int[] { 2, 1, 0, 4, 3 }, "dueDate_desc")]
    [InlineData(new int[] { 2, 3, 4, 0, 1 }, "priority")]
    [InlineData(new int[] { 1, 0, 4, 3, 2 }, "priority_desc")]
    [InlineData(new int[] { 0, 2, 1, 3, 4 }, "status")]
    [InlineData(new int[] { 4, 3, 1, 0, 2 }, "status_desc")]
    [InlineData(new int[] { 0, 1, 2, 3, 4 }, "createDate")]
    [InlineData(new int[] { 4, 3, 2, 1, 0 }, "createDate_desc")]
    [InlineData(new int[] { 0, 1, 2, 3, 4 }, "position")]
    [InlineData(new int[] { 4, 3, 2, 1, 0 }, "position_desc")]
    [InlineData(new int[] { 0, 1, 2, 3, 4 }, "summarry_desssc")]  // Invalid sortOrder. Defaults to position
    public async Task TestTasksSorting(int[] expectedOrder, string sortOrder)
    {
        var tasks = new List<TodoTaskDto>();
        tasks.Add(await CreateRandomTodoTask("Summary0", "Description1", DateTimeOffset.UtcNow.AddMinutes(2), 3, TodoTaskStatus.Pending));
        tasks.Add(await CreateRandomTodoTask("Summary1", "Description2", DateTimeOffset.UtcNow.AddMinutes(3), 4, TodoTaskStatus.Reserved));
        tasks.Add(await CreateRandomTodoTask("Summary2", "Description3", DateTimeOffset.UtcNow.AddMinutes(4), 0, TodoTaskStatus.Pending));
        tasks.Add(await CreateRandomTodoTask("Summary4", "Description4", DateTimeOffset.UtcNow.AddMinutes(0), 1, TodoTaskStatus.Ongoing));
        tasks.Add(await CreateRandomTodoTask("Summary3", "Description0", DateTimeOffset.UtcNow.AddMinutes(1), 2, TodoTaskStatus.Done));

        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>($"/tasks?pagenumber=1&pagesize=10&sortOrder={sortOrder}");

        var expectedReturnOrder = expectedOrder.Select(i => tasks[i]).ToList();
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(expectedReturnOrder, 1, 10, 5);
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestChangingTaskOrder()
    {
        var tasks = new List<TodoTaskDto>();
        var task1 = await CreateRandomTodoTask("Summary1");
        var task2 = await CreateRandomTodoTask("Summary2");
        var task3 = await CreateRandomTodoTask("Summary3");

        var moveDto = new MoveTodoTaskDto(0);
        var updateResponse = await _client.PutAsJsonAsync<MoveTodoTaskDto>($"/tasks/{task2.Id}/move", moveDto);
        updateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        task2.Position = task1.Position / 2;
        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>($"/tasks?pagenumber=1&pagesize=10");
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto> { task2, task1, task3 }, 1, 10, 3);
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestChangingSubTaskOrder()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task1_1 = await CreateRandomTodoTask("Summary1.1", parent: task1);
        var task1_2 = await CreateRandomTodoTask("Summary1.2", parent: task1);
        var task2 = await CreateRandomTodoTask("Summary2");
        var task2_1 = await CreateRandomTodoTask("Summary2.1", parent: task2);
        var task2_2 = await CreateRandomTodoTask("Summary2.2", parent: task2);
        var task2_3 = await CreateRandomTodoTask("Summary2.3", parent: task2);
        var task3 = await CreateRandomTodoTask("Summary3");
        var task3_1 = await CreateRandomTodoTask("Summary3.1", parent: task3);

        var moveDto = new MoveTodoTaskDto(0);
        var updateResponse = await _client.PutAsJsonAsync<MoveTodoTaskDto>($"/tasks/{task2_2.Id}/move", moveDto);
        updateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        task2_2.Position = task2_1.Position / 2;
        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>($"/tasks/{task2.Id}/subtasks?pagenumber=1&pagesize=10");
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto> { task2_2, task2_1, task2_3 }, 1, 10, 3);
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestRebalancingDuringTaskReordering()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task1_1 = await CreateRandomTodoTask("Summary1.1", parent: task1);
        var task1_2 = await CreateRandomTodoTask("Summary1.2", parent: task1);
        var task2 = await CreateRandomTodoTask("Summary2");
        var task2_1 = await CreateRandomTodoTask("Summary2.1", parent: task2);
        var task2_2 = await CreateRandomTodoTask("Summary2.2", parent: task2);
        var task3 = await CreateRandomTodoTask("Summary3");
        var task3_1 = await CreateRandomTodoTask("Summary3.1", parent: task3);

        var moveDto = new MoveTodoTaskDto(0);
        // 30 moves exhausts the available space between 0 and first tasks position. move #31 triggers rebalancing
        for (int i = 0; i < 32; i++)
        {
            var taskToMove = i % 2 == 0 ? task2 : task1;
            var updateResponse = await _client.PutAsJsonAsync<MoveTodoTaskDto>($"/tasks/{taskToMove.Id}/move", moveDto);

        }

        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>($"/tasks?pagenumber=1&pagesize=10");
        task1.Position = uint.MaxValue / 2;
        task2.Position = (ulong)uint.MaxValue;
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto> { task1, task2, task3 }, 1, 10, 3);
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestRebalancingSubTasksDuringTaskReordering()
    {
        var task1 = await CreateRandomTodoTask("Summary1");
        var task1_1 = await CreateRandomTodoTask("Summary1.1", parent: task1);
        var task1_2 = await CreateRandomTodoTask("Summary1.2", parent: task1);
        var task2 = await CreateRandomTodoTask("Summary2");
        var task2_1 = await CreateRandomTodoTask("Summary2.1", parent: task2);
        var task2_2 = await CreateRandomTodoTask("Summary2.2", parent: task2);
        var task2_3 = await CreateRandomTodoTask("Summary2.3", parent: task2);
        var task3 = await CreateRandomTodoTask("Summary3");
        var task3_1 = await CreateRandomTodoTask("Summary3.1", parent: task3);

        var moveDto = new MoveTodoTaskDto(0);
        // 30 moves exhausts the available space between 0 and first tasks position. move #31 triggers rebalancing
        for (int i = 0; i < 32; i++)
        {
            var taskToMove = i % 2 == 0 ? task2_2 : task2_1;
            var updateResponse = await _client.PutAsJsonAsync<MoveTodoTaskDto>($"/tasks/{taskToMove.Id}/move", moveDto);

        }

        var tasksResponse = await _client.GetFromJsonAsync<PaginatedResponse<TodoTaskDto>>($"/tasks/{task2.Id}/subtasks?pagenumber=1&pagesize=10");
        task2_1.Position = uint.MaxValue / 2;
        task2_2.Position = (ulong)uint.MaxValue;
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(new List<TodoTaskDto> { task2_1, task2_2, task2_3 }, 1, 10, 3);
        tasksResponse!.Should().BeEquivalentTo(expectedResponse);
    }

    private async Task<TodoTaskDto> CreateRandomTodoTask(string? summary = null,
                                                         string? description = null,
                                                         DateTimeOffset? dueDate = null,
                                                         int? priority = null,
                                                         TodoTaskStatus? status = null,
                                                         TodoTaskDto? parent = null)
    {
        {
            var statuses = Enum.GetValues<TodoTaskStatus>();
            var newTask = new CreateTodoTaskDto(
                summary: summary ?? Guid.NewGuid().ToString(),
                description: description ?? Guid.NewGuid().ToString(),
                dueDate: dueDate ?? DateTimeOffset.UtcNow,
                priority: priority ?? rand.Next(5),
                status: status ?? (TodoTaskStatus)rand.Next(statuses.Length),
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