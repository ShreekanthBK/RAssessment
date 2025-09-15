using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Backend.Data;
using Backend.Services;
using Backend.Models;
using System.Text;

namespace Backend.Tests.Services;

[TestFixture]
public class AttachmentServiceTests
{
    private TaskDbContext _context = null!;
    private AttachmentService _attachmentService = null!;
    private TestWebHostEnvironment _testEnvironment = null!;
    private string _testUploadPath = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TaskDbContext(options);
        
        // Setup test environment
        _testUploadPath = Path.Combine(Path.GetTempPath(), "test_uploads", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testUploadPath);
        _testEnvironment = new TestWebHostEnvironment { ContentRootPath = _testUploadPath };
        
        _attachmentService = new AttachmentService(_context, _testEnvironment);

        // Seed test data
        SeedTestData();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        
        // Clean up test files
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, true);
        }
    }

    private void SeedTestData()
    {
        var column = new TaskColumn { Id = 1, Name = "To Do", SortOrder = 1 };
        _context.Columns.Add(column);

        var task = new TaskItem
        {
            Id = 1,
            Name = "Test Task",
            ColumnId = 1,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();
    }

    [Test]
    public async Task UploadAttachmentAsync_WithValidFile_ShouldCreateAttachment()
    {
        // Arrange
        var mockFile = CreateTestFile("test.jpg", "image/jpeg", "test content");

        // Act
        var result = await _attachmentService.UploadAttachmentAsync(1, mockFile);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.FileName, Is.EqualTo("test.jpg"));
        Assert.That(result.ContentType, Is.EqualTo("image/jpeg"));
        Assert.That(result.FileSize, Is.EqualTo(12)); // "test content" length
        
        // Verify attachment was saved to database
        var attachmentInDb = await _context.Attachments
            .FirstOrDefaultAsync(a => a.FileName == "test.jpg");
        Assert.That(attachmentInDb, Is.Not.Null);
    }

    [Test]
    public void UploadAttachmentAsync_WithInvalidTaskId_ShouldThrowException()
    {
        // Arrange
        var mockFile = CreateTestFile("test.jpg", "image/jpeg", "test content");

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _attachmentService.UploadAttachmentAsync(999, mockFile));
        
        Assert.That(ex?.Message, Does.Contain("Task not found"));
    }

    [Test]
    public void UploadAttachmentAsync_WithEmptyFile_ShouldThrowException()
    {
        // Arrange
        var mockFile = CreateTestFile("empty.jpg", "image/jpeg", "");

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _attachmentService.UploadAttachmentAsync(1, mockFile));
        
        Assert.That(ex?.Message, Does.Contain("File is empty"));
    }

    [Test]
    public async Task DeleteAttachmentAsync_WithValidId_ShouldDeleteAttachment()
    {
        // Arrange - First create an attachment
        var mockFile = CreateTestFile("test.jpg", "image/jpeg", "test content");
        var attachment = await _attachmentService.UploadAttachmentAsync(1, mockFile);

        // Act
        var result = await _attachmentService.DeleteAttachmentAsync(attachment.Id);

        // Assert
        Assert.That(result, Is.True);
        
        var attachmentInDb = await _context.Attachments.FindAsync(attachment.Id);
        Assert.That(attachmentInDb, Is.Null);
    }

    [Test]
    public async Task DeleteAttachmentAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _attachmentService.DeleteAttachmentAsync(999);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DownloadAttachmentAsync_WithValidId_ShouldReturnFileStream()
    {
        // Arrange - First create an attachment
        var mockFile = CreateTestFile("test.jpg", "image/jpeg", "test content");
        var attachment = await _attachmentService.UploadAttachmentAsync(1, mockFile);

        // Act
        var result = await _attachmentService.DownloadAttachmentAsync(attachment.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.contentType, Is.EqualTo("image/jpeg"));
        Assert.That(result.Value.fileName, Is.EqualTo("test.jpg"));
        Assert.That(result.Value.stream, Is.Not.Null);
        
        // Verify stream content
        using var reader = new StreamReader(result.Value.stream);
        var content = await reader.ReadToEndAsync();
        Assert.That(content, Is.EqualTo("test content"));
    }

    [Test]
    public async Task DownloadAttachmentAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _attachmentService.DownloadAttachmentAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    private TestFormFile CreateTestFile(string fileName, string contentType, string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);
        return new TestFormFile(fileName, contentType, contentBytes);
    }

    // Simple test implementations to avoid Moq dependency
    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
    }

    private class TestFormFile : IFormFile
    {
        private readonly byte[] _content;

        public TestFormFile(string fileName, string contentType, byte[] content)
        {
            FileName = fileName;
            ContentType = contentType;
            _content = content;
            Length = content.Length;
        }

        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length { get; }
        public string Name => "file";
        public string FileName { get; }

        public Stream OpenReadStream() => new MemoryStream(_content);

        public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            return target.WriteAsync(_content, 0, _content.Length, cancellationToken);
        }
    }
}