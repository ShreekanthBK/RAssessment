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
public class AttachmentControllerIntegrationTests
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
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TaskDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<TaskDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("AttachmentTestDb");
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
    public async Task UploadAttachment_WithValidFile_ShouldReturnAttachmentResponse()
    {
        // Arrange - First create a task
        var taskRequest = new CreateTaskRequest
        {
            Name = "Task for Attachment",
            Description = "Test task",
            ColumnId = 1
        };

        var taskJson = JsonSerializer.Serialize(taskRequest);
        var taskContent = new StringContent(taskJson, Encoding.UTF8, "application/json");
        var taskResponse = await _client.PostAsync("/api/tasks", taskContent);
        var taskResponseContent = await taskResponse.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<TaskResponse>(taskResponseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Create a test file
        var fileContent = "Test file content";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        using var formData = new MultipartFormDataContent();
        using var fileStream = new MemoryStream(fileBytes);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        formData.Add(streamContent, "file", "test.jpg");

        // Act
        var response = await _client.PostAsync($"/api/attachments/tasks/{task!.Id}", formData);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var attachment = JsonSerializer.Deserialize<AttachmentResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.That(attachment, Is.Not.Null);
        Assert.That(attachment.FileName, Is.EqualTo("test.jpg"));
        Assert.That(attachment.ContentType, Is.EqualTo("image/jpeg"));
        Assert.That(attachment.FileSize, Is.EqualTo(fileBytes.Length));
    }

    [Test]
    public async Task UploadAttachment_WithInvalidTaskId_ShouldReturnBadRequest()
    {
        // Arrange
        var fileContent = "Test file content";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        using var formData = new MultipartFormDataContent();
        using var fileStream = new MemoryStream(fileBytes);
        using var streamContent = new StreamContent(fileStream);
        formData.Add(streamContent, "file", "test.jpg");

        // Act
        var response = await _client.PostAsync("/api/attachments/tasks/999", formData);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task DeleteAttachment_WithValidId_ShouldReturnNoContent()
    {
        // Arrange - Create task and attachment first
        var taskRequest = new CreateTaskRequest
        {
            Name = "Task for Attachment",
            ColumnId = 1
        };

        var taskJson = JsonSerializer.Serialize(taskRequest);
        var taskContent = new StringContent(taskJson, Encoding.UTF8, "application/json");
        var taskResponse = await _client.PostAsync("/api/tasks", taskContent);
        var taskResponseContent = await taskResponse.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<TaskResponse>(taskResponseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Upload attachment
        var fileBytes = Encoding.UTF8.GetBytes("Test content");
        using var formData = new MultipartFormDataContent();
        using var fileStream = new MemoryStream(fileBytes);
        using var streamContent = new StreamContent(fileStream);
        formData.Add(streamContent, "file", "test.jpg");

        var uploadResponse = await _client.PostAsync($"/api/attachments/tasks/{task!.Id}", formData);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var attachment = JsonSerializer.Deserialize<AttachmentResponse>(uploadContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/attachments/{attachment!.Id}");

        // Assert
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));
    }
}