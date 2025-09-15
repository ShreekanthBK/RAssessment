using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Services;

public class AttachmentService : IAttachmentService
{
    private readonly TaskDbContext _context;
    private readonly string _uploadPath;
    
    public AttachmentService(TaskDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _uploadPath = Path.Combine(environment.ContentRootPath, "uploads");
        
        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<AttachmentResponse> UploadAttachmentAsync(int taskId, IFormFile file)
    {
        // Validate task exists
        var taskExists = await _context.Tasks.AnyAsync(t => t.Id == taskId);
        if (!taskExists)
        {
            throw new ArgumentException("Task not found", nameof(taskId));
        }

        // Validate file
        if (file.Length == 0)
        {
            throw new ArgumentException("File is empty", nameof(file));
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Save attachment record
        var attachment = new TaskAttachment
        {
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            TaskId = taskId,
            UploadedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        return new AttachmentResponse
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            UploadedAt = attachment.UploadedAt
        };
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment == null) return false;

        // Delete file from disk
        if (File.Exists(attachment.FilePath))
        {
            File.Delete(attachment.FilePath);
        }

        // Delete record
        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(Stream stream, string contentType, string fileName)?> DownloadAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment == null || !File.Exists(attachment.FilePath))
        {
            return null;
        }

        var stream = new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);
        return (stream, attachment.ContentType, attachment.FileName);
    }
}
