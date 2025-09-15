using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Services;

public class TaskService : ITaskService
{
    private readonly TaskDbContext _context;
    
    public TaskService(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskResponse>> GetAllTasksAsync()
    {
        var tasks = await _context.Tasks
            .Include(t => t.Column)
            .Include(t => t.Attachments)
            .OrderBy(t => t.Column.SortOrder)
            .ThenBy(t => t.IsFavorite ? 0 : 1) // Favorites first
            .ThenBy(t => t.Name)
            .ToListAsync();

        return tasks.Select(MapToResponse);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(int id)
    {
        var task = await _context.Tasks
            .Include(t => t.Column)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        return task != null ? MapToResponse(task) : null;
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request)
    {
        // Get the next sort order for the column
        var maxSortOrder = await _context.Tasks
            .Where(t => t.ColumnId == request.ColumnId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        var task = new TaskItem
        {
            Name = request.Name,
            Description = request.Description,
            Deadline = request.Deadline,
            ColumnId = request.ColumnId,
            SortOrder = maxSortOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Reload with related data
        await _context.Entry(task)
            .Reference(t => t.Column)
            .LoadAsync();

        return MapToResponse(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(int id, UpdateTaskRequest request)
    {
        var task = await _context.Tasks
            .Include(t => t.Column)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return null;

        task.Name = request.Name;
        task.Description = request.Description;
        task.Deadline = request.Deadline;
        task.IsFavorite = request.IsFavorite;
        task.UpdatedAt = DateTime.UtcNow;

        // Handle column change
        if (task.ColumnId != request.ColumnId)
        {
            var maxSortOrder = await _context.Tasks
                .Where(t => t.ColumnId == request.ColumnId)
                .MaxAsync(t => (int?)t.SortOrder) ?? 0;

            task.ColumnId = request.ColumnId;
            task.SortOrder = maxSortOrder + 1;
        }

        await _context.SaveChangesAsync();

        // Reload column info
        await _context.Entry(task)
            .Reference(t => t.Column)
            .LoadAsync();

        return MapToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TaskResponse?> MoveTaskAsync(int id, MoveTaskRequest request)
    {
        var task = await _context.Tasks
            .Include(t => t.Column)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return null;

        // Update task position
        task.ColumnId = request.ColumnId;
        task.SortOrder = request.SortOrder;
        task.UpdatedAt = DateTime.UtcNow;

        // Reorder other tasks in the target column
        var tasksToReorder = await _context.Tasks
            .Where(t => t.ColumnId == request.ColumnId && t.Id != id && t.SortOrder >= request.SortOrder)
            .ToListAsync();

        foreach (var taskToReorder in tasksToReorder)
        {
            taskToReorder.SortOrder++;
        }

        await _context.SaveChangesAsync();

        // Reload column info
        await _context.Entry(task)
            .Reference(t => t.Column)
            .LoadAsync();

        return MapToResponse(task);
    }

    public async Task<IEnumerable<TaskResponse>> GetTasksByColumnAsync(int columnId)
    {
        var tasks = await _context.Tasks
            .Include(t => t.Column)
            .Include(t => t.Attachments)
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.IsFavorite ? 0 : 1) // Favorites first
            .ThenBy(t => t.Name)
            .ToListAsync();

        return tasks.Select(MapToResponse);
    }

    private static TaskResponse MapToResponse(TaskItem task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            Deadline = task.Deadline,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            IsFavorite = task.IsFavorite,
            ColumnId = task.ColumnId,
            ColumnName = task.Column?.Name ?? string.Empty,
            SortOrder = task.SortOrder,
            Attachments = task.Attachments.Select(a => new AttachmentResponse
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedAt = a.UploadedAt
            }).ToList()
        };
    }
}
