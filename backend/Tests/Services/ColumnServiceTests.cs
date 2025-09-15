using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services;
using Backend.Models;
using Backend.DTOs;

namespace Backend.Tests.Services;

[TestFixture]
public class ColumnServiceTests
{
    private TaskDbContext _context;
    private ColumnService _columnService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TaskDbContext(options);
        _columnService = new ColumnService(_context);

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

        _context.Columns.AddRange(column1, column2);
        _context.SaveChanges();
    }

    [Test]
    public async Task GetAllColumnsAsync_ShouldReturnColumnsOrderedBySortOrder()
    {
        // Act
        var result = await _columnService.GetAllColumnsAsync();
        var columns = result.ToList();

        // Assert
        Assert.That(columns, Has.Count.EqualTo(2));
        Assert.That(columns[0].Name, Is.EqualTo("To Do"));
        Assert.That(columns[1].Name, Is.EqualTo("In Progress"));
        Assert.That(columns[0].SortOrder, Is.LessThan(columns[1].SortOrder));
    }

    [Test]
    public async Task GetColumnByIdAsync_WithValidId_ShouldReturnColumn()
    {
        // Act
        var result = await _columnService.GetColumnByIdAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("To Do"));
    }

    [Test]
    public async Task GetColumnByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _columnService.GetColumnByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateColumnAsync_ShouldCreateColumnWithCorrectSortOrder()
    {
        // Arrange
        var request = new CreateColumnRequest { Name = "Done" };

        // Act
        var result = await _columnService.CreateColumnAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Done"));
        Assert.That(result.SortOrder, Is.EqualTo(3)); // Should be after existing columns
        
        var columnInDb = await _context.Columns.FindAsync(result.Id);
        Assert.That(columnInDb, Is.Not.Null);
    }

    [Test]
    public async Task DeleteColumnAsync_WithEmptyColumn_ShouldDeleteSuccessfully()
    {
        // Act
        var result = await _columnService.DeleteColumnAsync(2);

        // Assert
        Assert.That(result, Is.True);
        
        var columnInDb = await _context.Columns.FindAsync(2);
        Assert.That(columnInDb, Is.Null);
    }

    [Test]
    public async Task DeleteColumnAsync_WithTasks_ShouldThrowException()
    {
        // Arrange - Add a task to column 1
        var task = new TaskItem
        {
            Name = "Test Task",
            ColumnId = 1,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _columnService.DeleteColumnAsync(1));
        
        Assert.That(ex.Message, Does.Contain("Cannot delete column with existing tasks"));
    }

    [Test]
    public async Task GetBoardAsync_ShouldReturnCompleteBoard()
    {
        // Act
        var result = await _columnService.GetBoardAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Columns, Has.Count.EqualTo(2));
        Assert.That(result.Columns[0].Name, Is.EqualTo("To Do"));
        Assert.That(result.Columns[1].Name, Is.EqualTo("In Progress"));
    }
}
