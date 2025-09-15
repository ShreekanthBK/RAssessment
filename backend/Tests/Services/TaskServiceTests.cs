using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services;
using Backend.Models;
using Backend.DTOs;

namespace Backend.Tests.Services;

[TestFixture]
public class TaskServiceTests
{
    private TaskDbContext _context;
    private TaskService _taskService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TaskDbContext(options);
        _taskService = new TaskService(_context);

        // Seed test data
        SeedTestData();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var column1 = new TaskColumn { Id = 1, Name = "To Do", SortOrder = 1 };
        var column2 = new TaskColumn { Id = 2, Name = "In Progress", SortOrder = 2 };
        var column3 = new TaskColumn { Id = 3, Name = "Done", SortOrder = 3 };

        _context.Columns.AddRange(column1, column2, column3);

        var task1 = new TaskItem
        {
            Id = 1,
            Name = "Test Task 1",
            Description = "Description 1",
            ColumnId = 1,
            SortOrder = 1,
            IsFavorite = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var task2 = new TaskItem
        {
            Id = 2,
            Name = "Favorite Task",
            Description = "Favorite Description",
            ColumnId = 1,
            SortOrder = 2,
            IsFavorite = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.AddRange(task1, task2);
        _context.SaveChanges();
    }

    [Test]
    public async Task GetAllTasksAsync_ShouldReturnTasksOrderedCorrectly()
    {
        // Act
        var result = await _taskService.GetAllTasksAsync();
        var tasks = result.ToList();

        // Assert
        Assert.That(tasks, Has.Count.EqualTo(2));
        Assert.That(tasks[0].IsFavorite, Is.True, "Favorite tasks should be sorted first");
        Assert.That(tasks[0].Name, Is.EqualTo("Favorite Task"));
    }

    [Test]
    public async Task GetTaskByIdAsync_WithValidId_ShouldReturnTask()
    {
        // Act
        var result = await _taskService.GetTaskByIdAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Test Task 1"));
    }

    [Test]
    public async Task GetTaskByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _taskService.GetTaskByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateTaskAsync_ShouldCreateTaskSuccessfully()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "New Task",
            Description = "New Description",
            Deadline = DateTime.Now.AddDays(7),
            ColumnId = 1
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("New Task"));
        Assert.That(result.SortOrder, Is.EqualTo(3)); // Should be after existing tasks
        
        var taskInDb = await _context.Tasks.FindAsync(result.Id);
        Assert.That(taskInDb, Is.Not.Null);
    }

    [Test]
    public async Task UpdateTaskAsync_ShouldUpdateTaskSuccessfully()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Name = "Updated Task",
            Description = "Updated Description",
            IsFavorite = true,
            ColumnId = 2
        };

        // Act
        var result = await _taskService.UpdateTaskAsync(1, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Updated Task"));
        Assert.That(result.IsFavorite, Is.True);
        Assert.That(result.ColumnId, Is.EqualTo(2));
    }

    [Test]
    public async Task DeleteTaskAsync_WithValidId_ShouldDeleteTask()
    {
        // Act
        var result = await _taskService.DeleteTaskAsync(1);

        // Assert
        Assert.That(result, Is.True);
        
        var taskInDb = await _context.Tasks.FindAsync(1);
        Assert.That(taskInDb, Is.Null);
    }

    [Test]
    public async Task DeleteTaskAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _taskService.DeleteTaskAsync(999);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task MoveTaskAsync_ShouldMoveTaskToNewColumn()
    {
        // Arrange
        var moveRequest = new MoveTaskRequest
        {
            ColumnId = 2,
            SortOrder = 1
        };

        // Act
        var result = await _taskService.MoveTaskAsync(1, moveRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ColumnId, Is.EqualTo(2));
        Assert.That(result.SortOrder, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTasksByColumnAsync_ShouldReturnTasksForColumn()
    {
        // Act
        var result = await _taskService.GetTasksByColumnAsync(1);
        var tasks = result.ToList();

        // Assert
        Assert.That(tasks, Has.Count.EqualTo(2));
        Assert.That(tasks.All(t => t.ColumnId == 1), Is.True);
        
        // Verify favorite sorting
        Assert.That(tasks[0].IsFavorite, Is.True);
        Assert.That(tasks[1].IsFavorite, Is.False);
    }

    [Test]
    public async Task GetAllTasksAsync_WithMixedFavoritesAndAlphabetical_ShouldSortCorrectly()
    {
        // Arrange - Create additional tasks for comprehensive sorting test
        await _taskService.CreateTaskAsync(new CreateTaskRequest 
        { 
            Name = "Alpha Task", 
            Description = "Should be second", 
            ColumnId = 1 
        });
        
        await _taskService.CreateTaskAsync(new CreateTaskRequest 
        { 
            Name = "Beta Favorite", 
            Description = "Should be first", 
            ColumnId = 1 
        });

        // Make "Beta Favorite" a favorite
        var tasks = await _taskService.GetAllTasksAsync();
        var betaTask = tasks.First(t => t.Name == "Beta Favorite");
        await _taskService.UpdateTaskAsync(betaTask.Id, new UpdateTaskRequest
        {
            Name = "Beta Favorite",
            Description = "Should be first",
            IsFavorite = true,
            ColumnId = 1
        });

        // Act
        var result = await _taskService.GetAllTasksAsync();
        var sortedTasks = result.ToList();

        // Assert - Verify favorites come first, then alphabetical
        var favoriteTasks = sortedTasks.Where(t => t.IsFavorite).ToList();
        var nonFavoriteTasks = sortedTasks.Where(t => !t.IsFavorite).ToList();

        // Check favorites are first
        Assert.That(favoriteTasks.Count, Is.GreaterThan(0));
        Assert.That(sortedTasks.Take(favoriteTasks.Count).All(t => t.IsFavorite), Is.True);

        // Check alphabetical order within favorites
        for (int i = 1; i < favoriteTasks.Count; i++)
        {
            Assert.That(string.Compare(favoriteTasks[i-1].Name, favoriteTasks[i].Name, StringComparison.OrdinalIgnoreCase), 
                       Is.LessThanOrEqualTo(0), 
                       $"Favorite tasks not in alphabetical order: {favoriteTasks[i-1].Name} should come before {favoriteTasks[i].Name}");
        }

        // Check alphabetical order within non-favorites
        for (int i = 1; i < nonFavoriteTasks.Count; i++)
        {
            Assert.That(string.Compare(nonFavoriteTasks[i-1].Name, nonFavoriteTasks[i].Name, StringComparison.OrdinalIgnoreCase), 
                       Is.LessThanOrEqualTo(0), 
                       $"Non-favorite tasks not in alphabetical order: {nonFavoriteTasks[i-1].Name} should come before {nonFavoriteTasks[i].Name}");
        }
    }

    [Test]
    public async Task CreateTaskAsync_WithDeadline_ShouldStoreDeadlineCorrectly()
    {
        // Arrange
        var deadline = DateTime.Now.AddDays(7);
        var request = new CreateTaskRequest
        {
            Name = "Task with Deadline",
            Description = "Has a deadline",
            Deadline = deadline,
            ColumnId = 1
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request);

        // Assert
        Assert.That(result.Deadline, Is.Not.Null);
        Assert.That(result.Deadline.Value.Date, Is.EqualTo(deadline.Date));
    }

    [Test]
    public async Task UpdateTaskAsync_MarkingAsFavorite_ShouldUpdateFavoriteStatus()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Name = "Test Task 1",
            Description = "Description 1",
            IsFavorite = true,
            ColumnId = 1
        };

        // Act
        var result = await _taskService.UpdateTaskAsync(1, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsFavorite, Is.True);
        
        // Verify in database
        var taskInDb = await _context.Tasks.FindAsync(1);
        Assert.That(taskInDb.IsFavorite, Is.True);
    }

    [Test]
    public async Task MoveTaskAsync_BetweenColumns_ShouldUpdateSortOrderCorrectly()
    {
        // Arrange - Create multiple tasks in target column
        await _taskService.CreateTaskAsync(new CreateTaskRequest 
        { 
            Name = "Existing Task 1", 
            ColumnId = 2 
        });
        await _taskService.CreateTaskAsync(new CreateTaskRequest 
        { 
            Name = "Existing Task 2", 
            ColumnId = 2 
        });

        var moveRequest = new MoveTaskRequest
        {
            ColumnId = 2,
            SortOrder = 1
        };

        // Act
        var result = await _taskService.MoveTaskAsync(1, moveRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ColumnId, Is.EqualTo(2));
        Assert.That(result.SortOrder, Is.EqualTo(1));

        // Verify other tasks were reordered
        var tasksInColumn = await _taskService.GetTasksByColumnAsync(2);
        var tasksList = tasksInColumn.ToList();
        
        // Should have 3 tasks now (2 existing + 1 moved)
        Assert.That(tasksList.Count, Is.EqualTo(3));
    }
}
