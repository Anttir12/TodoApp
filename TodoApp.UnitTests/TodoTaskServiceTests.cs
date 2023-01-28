namespace TodoApp.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using TodoApp.Dtos;
using TodoApp.Exceptions;
using TodoApp.Models;
using TodoApp.Pagination;
using TodoApp.Repositories;
using TodoApp.Services;
using Xunit;

public class TodoTaskServiceTests
{
    private readonly Mock<IRepository> mockedRepository = new();
    private readonly Random rand = new();

    [Fact]
    public async Task CreateTodoTaskAsync_WithoutParent_ReturnsTodoTaskDto()
    {
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync((TodoTask)null!);
        var service = new TodoTaskService(mockedRepository.Object);

        var createDto = new CreateTodoTaskDto(
            summary: Guid.NewGuid().ToString(),
            description: Guid.NewGuid().ToString(),
            dueDate: DateTimeOffset.UtcNow,
            priority: rand.Next(3),
            status: TodoTaskStatus.Ongoing,
            parentId: null
        );
        var result = await service.CreateTodoTaskAsync(createDto);

        result.Should().BeEquivalentTo<CreateTodoTaskDto>(createDto);
    }

    [Fact]
    public async Task CreateTodoTaskAsync_WithParent_ReturnsTodoTaskDto()
    {
        var parent = CreateRandomTask();
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync(parent);
        var service = new TodoTaskService(mockedRepository.Object);

        var createDto = new CreateTodoTaskDto(
            summary: Guid.NewGuid().ToString(),
            description: Guid.NewGuid().ToString(),
            dueDate: DateTimeOffset.UtcNow,
            priority: rand.Next(3),
            status: TodoTaskStatus.Ongoing,
            parentId: parent.Id
        );


        var result = await service.CreateTodoTaskAsync(createDto);

        result.Should().BeEquivalentTo<CreateTodoTaskDto>(createDto);
    }

    [Fact]
    public async Task CreateTodoTaskAsync_WithInvalidParent_ReturnsTodoTaskDto()
    {
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TodoTask)null!);
        var service = new TodoTaskService(mockedRepository.Object);

        var createDto = new CreateTodoTaskDto(
            summary: Guid.NewGuid().ToString(),
            description: Guid.NewGuid().ToString(),
            dueDate: DateTimeOffset.UtcNow,
            priority: rand.Next(3),
            status: TodoTaskStatus.Ongoing,
            parentId: Guid.NewGuid()
        );

        await service.Invoking(async r => await r.CreateTodoTaskAsync(createDto))
            .Should().ThrowAsync<InvalidForeignKeyException>()
            .WithMessage("Parent task not found");
    }

    [Fact]
    public async Task DeleteTodoTaskAsync_WithValidId_ReturnsTrue()
    {
        var task = CreateRandomTask();
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync(task);
        var service = new TodoTaskService(mockedRepository.Object);

        bool deleted = await service.DeleteTodoTaskAsync(Guid.NewGuid());

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTodoTaskAsync_WithInvalidId_ReturnsFalse()
    {
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TodoTask)null!);
        var service = new TodoTaskService(mockedRepository.Object);

        bool deleted = await service.DeleteTodoTaskAsync(Guid.NewGuid());

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetTodoTaskAsync_WithValidId_ReturnsTask()
    {
        var task = CreateRandomTask();
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync(task);
        var service = new TodoTaskService(mockedRepository.Object);

        var returnedTask = await service.GetTodoTaskAsync(Guid.NewGuid());

        returnedTask.Should().BeEquivalentTo(task.asDto());
    }

    [Fact]
    public async Task GetTodoTaskAsync_WithInvalidId_ReturnsNull()
    {
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TodoTask)null!);
        var service = new TodoTaskService(mockedRepository.Object);

        var returnedTask = await service.GetTodoTaskAsync(Guid.NewGuid());

        returnedTask.Should().BeNull();
    }

    [Fact]
    public async Task GetTodoTasksAsync_WithResults_ReturnsPaginationResposeWithTasksPageSizeCount()
    {
        var tasks = new List<TodoTask>();
        tasks.Add(CreateRandomTask());
        tasks.Add(CreateRandomTask());
        tasks.Add(CreateRandomTask());
        mockedRepository.Setup(r => r.GetTodoTasksAsync(It.IsAny<PaginationFilter>(),
                It.IsAny<Expression<Func<TodoTask, bool>>>()))
            .ReturnsAsync((tasks, 4));
        var service = new TodoTaskService(mockedRepository.Object);

        PaginationFilter paginationFilter = new PaginationFilter(1, 3);
        var paginatedResponse = await service.GetTodoTasksAsync(paginationFilter, (t => t.ParentId == null));

        var dtoTasks = tasks.Select(t => t.asDto()).ToList();
        var expectedPaginatedResponse = new PaginatedResponse<TodoTaskDto>(dtoTasks, 1, 3, 4);
        paginatedResponse.Should().BeEquivalentTo(expectedPaginatedResponse);
    }

    [Fact]
    public async Task GetTodoTasksAsync_WithNoResults_ReturnsPaginationResposeWithNoTasksZeroCount()
    {
        var tasks = new List<TodoTask>();
        mockedRepository.Setup(r => r.GetTodoTasksAsync(It.IsAny<PaginationFilter>(),
                It.IsAny<Expression<Func<TodoTask, bool>>>()))
            .ReturnsAsync((tasks, 0));
        var service = new TodoTaskService(mockedRepository.Object);

        PaginationFilter paginationFilter = new PaginationFilter(1, 3);
        var paginatedResponse = await service.GetTodoTasksAsync(paginationFilter, (t => t.ParentId == null));

        var dtoTasks = tasks.Select(t => t.asDto()).ToList();
        var expectedPaginatedResponse = new PaginatedResponse<TodoTaskDto>(dtoTasks, 1, 3, 0);
        paginatedResponse.Should().BeEquivalentTo(expectedPaginatedResponse);
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_MoveChildToTopLevel_ReturnsNoContent()
    {

        TodoTask parentTask = CreateRandomTask();
        TodoTask childTask = CreateRandomTask(parentTask);
        mockedRepository.Setup(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync(childTask);

        var service = new TodoTaskService(mockedRepository.Object);
        var taskToUpdate = new UpdateTodoTaskDto(
            summary: "updated summary",
            description: null,
            dueDate: DateTimeOffset.UtcNow,
            priority: parentTask.Priority + 1,
            status: TodoTaskStatus.Done,
            parentId: null
        );

        var result = await service.UpdateTodoTaskAsync(childTask.Id, taskToUpdate);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_WithInvalidParentId_ThrowsInvalidForeignKeyException()
    {
        TodoTask existingTask = CreateRandomTask();
        mockedRepository.SetupSequence(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync(existingTask) // First getting the task to be updated
            .ReturnsAsync((TodoTask)null!);  // Parent does not exist


        var service = new TodoTaskService(mockedRepository.Object);
        var taskToUpdate = new UpdateTodoTaskDto(
            summary: "updated summary",
            description: null,
            dueDate: DateTimeOffset.UtcNow,
            priority: 2,
            status: TodoTaskStatus.Done,
            parentId: Guid.NewGuid()
        );

        await service.Invoking(async r => await r.UpdateTodoTaskAsync(Guid.NewGuid(), taskToUpdate))
            .Should().ThrowAsync<InvalidForeignKeyException>()
            .WithMessage("Parent task not found");
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_WithCircularParentChild_ThrowsInvalidForeignKeyException()
    {

        TodoTask parentTask = CreateRandomTask();
        TodoTask childTask = CreateRandomTask(parentTask);
        mockedRepository.SetupSequence(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
            .ReturnsAsync(parentTask) // First getting the task to be updated
            .ReturnsAsync(childTask);  // Then the new Parent for parentTask

        var service = new TodoTaskService(mockedRepository.Object);
        var taskToUpdate = new UpdateTodoTaskDto(
            summary: "updated summary",
            description: null,
            dueDate: DateTimeOffset.UtcNow,
            priority: parentTask.Priority + 1,
            status: TodoTaskStatus.Done,
            parentId: childTask.Id
        );

        await service.Invoking(async r => await r.UpdateTodoTaskAsync(Guid.NewGuid(), taskToUpdate))
            .Should().ThrowAsync<InvalidForeignKeyException>()
            .WithMessage("Updating ParentID would cause circular relationship!");
    }

    private TodoTask CreateRandomTask(TodoTask? parent = null)
    {
        var statuses = Enum.GetValues<TodoTaskStatus>();
        var newTask = new TodoTask(
            id: Guid.NewGuid(),
            summary: Guid.NewGuid().ToString(),
            description: Guid.NewGuid().ToString(),
            createDate: DateTimeOffset.UtcNow,
            dueDate: DateTimeOffset.UtcNow,
            priority: rand.Next(5),
            status: (TodoTaskStatus)rand.Next(statuses.Length),
            position: uint.MaxValue,
            parentId: parent?.Id
        );
        if (parent != null)
        {
            parent.SubTaskCount += 1;
        }

        return newTask;
    }
}