namespace TodoApp.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApp.Controllers;
using TodoApp.Dtos;
using TodoApp.Models;
using TodoApp.Pagination;
using TodoApp.Services;
using Xunit;

public class TaskControllerTest
{
    private readonly Mock<ITodoTaskService> mockedService = new();
    private readonly Random rand = new();

    [Fact]
    public async Task GetAsync_WithNonexistingTask_ReturnsNotFound()
    {
        mockedService.Setup(s => s.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync((TodoTaskDto)null!);
        var controller = new TasksController(mockedService.Object);

        var result = await controller.GetTodoTaskAsync(Guid.NewGuid());
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTodoTaskAsync_WithExistingTask_ReturnsTask()
    {
        var expectedTask = CreateRandomTask();
        mockedService.Setup(s => s.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync(expectedTask.asDto());
        var controller = new TasksController(mockedService.Object);

        var result = await controller.GetTodoTaskAsync(expectedTask.Id);

        result.Value.Should().BeEquivalentTo(
            expectedTask.asDto(),
            options => options.ComparingByMembers<TodoTaskDto>());
    }

    [Fact]
    public async Task GetTodoTaskAsync_TaskWithSubtasks_ReturnsTaskWithSubtaskCount1()
    {
        var expectedTask = CreateRandomTask();
        var subTask1 = CreateRandomTask(expectedTask);
        mockedService.Setup(s => s.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync(expectedTask.asDto());
        var controller = new TasksController(mockedService.Object);

        var result = await controller.GetTodoTaskAsync(expectedTask.Id);

        result.Value.Should().BeEquivalentTo(
            expectedTask.asDto(),
            options => options.ComparingByMembers<TodoTaskDto>());
    }

    [Fact]
    public async Task GetTodoTasksAsync_ExistingTasks_ReturnsTasks()
    {
        var parentTask1 = CreateRandomTask(null);
        var subTask1 = CreateRandomTask(parentTask1);
        var expectedTasks = new[] { parentTask1.asDto(), CreateRandomTask().asDto(), CreateRandomTask().asDto() };
        var expectedResponse = new PaginatedResponse<TodoTaskDto>(expectedTasks, 1, 10, 3);

        mockedService.Setup(s => s.GetTodoTasksAsync(It.IsAny<PaginationFilter>(), It.IsAny<Expression<Func<TodoTask, bool>>>()))
        .ReturnsAsync(expectedResponse);
        var controller = new TasksController(mockedService.Object);

        var result = await controller.GetTodoTasksAsync(new PaginationFilter());

        result.Value!.Should().BeEquivalentTo(
            expectedResponse,
            options => options.ComparingByMembers<PaginatedResponse<TodoTaskDto>>());
    }

    [Fact]
    public async Task CreateTodoTaskAsync_WithValidTask_ReturnsCreatedTasks()
    {
        var createDto = new CreateTodoTaskDto(
            summary: Guid.NewGuid().ToString(),
            description: Guid.NewGuid().ToString(),
            dueDate: DateTimeOffset.UtcNow,
            priority: rand.Next(3),
            status: TodoTaskStatus.Ongoing,
            parentId: null
        );

        var expectedTask = new TodoTask
        (
            Guid.NewGuid(),
            createDto.Summary,
            createDto.Description,
            DateTimeOffset.UtcNow,
            createDto.DueDate,
            createDto.Priority,
            createDto.Status,
            createDto.ParentId
        );

        mockedService.Setup(s => s.CreateTodoTaskAsync(It.IsAny<CreateTodoTaskDto>()))
        .ReturnsAsync(expectedTask.asDto());
        var controller = new TasksController(mockedService.Object);

        var result = await controller.CreateTodoTaskAsync(createDto);

        var createdTask = (result.Result! as CreatedAtActionResult)!.Value as TodoTaskDto;

        createDto.Should().BeEquivalentTo(
            createdTask,
            options => options.ComparingByMembers<TodoTaskDto>().ExcludingMissingMembers()
        );
        createdTask!.Id.Should().NotBeEmpty();
        createdTask.CreateDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_WithExistingTask_ReturnsNoContent()
    {
        TodoTask existingTask = CreateRandomTask();
        mockedService.Setup(s => s.UpdateTodoTask(It.IsAny<Guid>(), It.IsAny<UpdateTodoTaskDto>()))
        .ReturnsAsync(true);

        var controller = new TasksController(mockedService.Object);

        var result = await controller.UpdateTodoTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTodoTaskDto>());
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_WithNonexistingTask_ReturnsNotFound()
    {
        mockedService.Setup(s => s.UpdateTodoTask(It.IsAny<Guid>(), It.IsAny<UpdateTodoTaskDto>()))
        .ReturnsAsync(false);

        var controller = new TasksController(mockedService.Object);

        var result = await controller.UpdateTodoTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTodoTaskDto>());
        result.Should().BeOfType<NotFoundResult>();
    }
    /* TODO: Move to service unit tests

    [Fact]
    public async Task UpdateTodoTaskAsync_MoveChildToTopLevel_ReturnsNoContent()
    {

        TodoTask parentTask = CreateRandomTask();
        TodoTask childTask = CreateRandomTask(parentTask);
        mockedService.Setup(s => s.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync(childTask.asDto());

        var controller = new TasksController(mockedService.Object);
        var taskToUpdate = new UpdateTodoTaskDto(
            summary: "updated summary",
            description: null,
            dueDate: DateTimeOffset.UtcNow,
            priority: parentTask.Priority + 1,
            status: TodoTaskStatus.Done,
            parentId: null
        );

        var result = await controller.UpdateTodoTaskAsync(childTask.Id, taskToUpdate);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_WithInvalidParentId_ReturnsBadRequest()
    {
        TodoTask existingTask = CreateRandomTask();
        mockedService.SetupSequence(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync(existingTask.asDto()) // First getting the task to be updated
        .ReturnsAsync((TodoTaskDto)null!);  // Parent does not exist


        var controller = new TasksController(mockedService.Object);
        var taskToUpdate = new UpdateTodoTaskDto(
            summary: "updated summary",
            description: null,
            dueDate: DateTimeOffset.UtcNow,
            priority: 2,
            status: TodoTaskStatus.Done,
            parentId: Guid.NewGuid()
        );

        var result = await controller.UpdateTodoTaskAsync(Guid.NewGuid(), taskToUpdate);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateTodoTaskAsync_WithCircularParentChild_ReturnsBadRequest()
    {

        TodoTask parentTask = CreateRandomTask();
        TodoTask childTask = CreateRandomTask(parentTask);
        mockedService.SetupSequence(r => r.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync(parentTask.asDto()) // First getting the task to be updated
        .ReturnsAsync(childTask.asDto());  // Then the new Parent for parentTask

        var controller = new TasksController(mockedService.Object);
        var taskToUpdate = new UpdateTodoTaskDto(
            summary: "updated summary",
            description: null,
            dueDate: DateTimeOffset.UtcNow,
            priority: parentTask.Priority + 1,
            status: TodoTaskStatus.Done,
            parentId: childTask.Id
        );

        var result = await controller.UpdateTodoTaskAsync(parentTask.Id, taskToUpdate);
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    */
    [Fact]
    public async Task DeleteTodoTaskAsync_ExistingTask_ReturnsNoContent()
    {
        TodoTask existingTask = CreateRandomTask();

        mockedService.Setup(s => s.DeleteTodoTask(It.IsAny<Guid>()))
        .ReturnsAsync(true);

        var controller = new TasksController(mockedService.Object);

        var result = await controller.DeleteTodoTaskAsync(existingTask.Id);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTodoTaskAsync_NonExistingTask_ReturnsNotFound()
    {
        mockedService.Setup(s => s.GetTodoTaskAsync(It.IsAny<Guid>()))
        .ReturnsAsync((TodoTaskDto)null!);

        var controller = new TasksController(mockedService.Object);

        var result = await controller.DeleteTodoTaskAsync(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
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
            parentId: parent?.Id
        );
        if (parent != null)
        {
            parent.SubTaskCount += 1;
        }

        return newTask;
    }
}