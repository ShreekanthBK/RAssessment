using NUnit.Framework;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Backend.DTOs;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Integration;

[TestFixture]
public class TasksControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TaskDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add a database context using an in-memory database for testing
                    services.AddDbContext<TaskDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetBoard_ShouldReturnDefaultColumns()
    {
        // Act
        var response = await _client.GetAsync("/api/board");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(board, Is.Not.Null);
        Assert.That(board.Columns, Has.Count.EqualTo(3));
        Assert.That(board.Columns.Any(c => c.Name == "To Do"), Is.True);
        Assert.That(board.Columns.Any(c => c.Name == "In Progress"), Is.True);
        Assert.That(board.Columns.Any(c => c.Name == "Done"), Is.True);
    }

    [Test]
    public async Task CreateTask_ShouldCreateTaskSuccessfully()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "Integration Test Task",
            Description = "Test Description",
            Deadline = DateTime.Now.AddDays(7),
            ColumnId = 1
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/tasks", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<TaskResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(task, Is.Not.Null);
        Assert.That(task.Name, Is.EqualTo("Integration Test Task"));
        Assert.That(task.ColumnId, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTasks_ShouldReturnCreatedTask()
    {
        // Arrange - First create a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Test Task for Get",
            Description = "Test Description",
            ColumnId = 1
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        await _client.PostAsync("/api/tasks", content);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var tasks = JsonSerializer.Deserialize<List<TaskResponse>>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(tasks, Is.Not.Null);
        Assert.That(tasks.Count, Is.GreaterThan(0));
        Assert.That(tasks.Any(t => t.Name == "Test Task for Get"), Is.True);
    }

    [Test]
    public async Task UpdateTask_ShouldUpdateTaskSuccessfully()
    {
        // Arrange - First create a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Original Task",
            Description = "Original Description",
            ColumnId = 1
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        
        var createResponse = await _client.PostAsync("/api/tasks", createContent);
        var createdTaskContent = await createResponse.Content.ReadAsStringAsync();
        var createdTask = JsonSerializer.Deserialize<TaskResponse>(createdTaskContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Update the task
        var updateRequest = new UpdateTaskRequest
        {
            Name = "Updated Task",
            Description = "Updated Description",
            IsFavorite = true,
            ColumnId = 2
        };

        var updateJson = JsonSerializer.Serialize(updateRequest);
        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        // Act
        var updateResponse = await _client.PutAsync($"/api/tasks/{createdTask.Id}", updateContent);

        // Assert
        updateResponse.EnsureSuccessStatusCode();
        var updatedTaskContent = await updateResponse.Content.ReadAsStringAsync();
        var updatedTask = JsonSerializer.Deserialize<TaskResponse>(updatedTaskContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(updatedTask, Is.Not.Null);
        Assert.That(updatedTask.Name, Is.EqualTo("Updated Task"));
        Assert.That(updatedTask.IsFavorite, Is.True);
        Assert.That(updatedTask.ColumnId, Is.EqualTo(2));
    }

    [Test]
    public async Task DeleteTask_ShouldDeleteTaskSuccessfully()
    {
        // Arrange - First create a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Task to Delete",
            Description = "Will be deleted",
            ColumnId = 1
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        
        var createResponse = await _client.PostAsync("/api/tasks", createContent);
        var createdTaskContent = await createResponse.Content.ReadAsStringAsync();
        var createdTask = JsonSerializer.Deserialize<TaskResponse>(createdTaskContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{createdTask.Id}");

        // Assert
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));

        // Verify task is deleted
        var getResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }
}
