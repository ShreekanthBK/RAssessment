using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Services;

public class ColumnService : IColumnService
{
    private readonly TaskDbContext _context;
    
    public ColumnService(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ColumnResponse>> GetAllColumnsAsync()
    {
        var columns = await _context.Columns
            .Include(c => c.Tasks)
                .ThenInclude(t => t.Attachments)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return columns.Select(MapToResponse);
    }

    public async Task<ColumnResponse?> GetColumnByIdAsync(int id)
    {
        var column = await _context.Columns
            .Include(c => c.Tasks)
                .ThenInclude(t => t.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id);

        return column != null ? MapToResponse(column) : null;
    }

    public async Task<ColumnResponse> CreateColumnAsync(CreateColumnRequest request)
    {
        var maxSortOrder = await _context.Columns
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var column = new TaskColumn
        {
            Name = request.Name,
            SortOrder = maxSortOrder + 1
        };

        _context.Columns.Add(column);
        await _context.SaveChangesAsync();

        return MapToResponse(column);
    }

    public async Task<bool> DeleteColumnAsync(int id)
    {
        var column = await _context.Columns
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (column == null) return false;

        // Don't delete if it has tasks
        if (column.Tasks.Any())
        {
            throw new InvalidOperationException("Cannot delete column with existing tasks");
        }

        _context.Columns.Remove(column);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BoardResponse> GetBoardAsync()
    {
        var columns = await GetAllColumnsAsync();
        return new BoardResponse { Columns = columns.ToList() };
    }

    private static ColumnResponse MapToResponse(TaskColumn column)
    {
        var tasks = column.Tasks
            .OrderBy(t => t.IsFavorite ? 0 : 1) // Favorites first
            .ThenBy(t => t.Name)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Deadline = t.Deadline,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                IsFavorite = t.IsFavorite,
                ColumnId = t.ColumnId,
                ColumnName = column.Name,
                SortOrder = t.SortOrder,
                Attachments = t.Attachments.Select(a => new AttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    UploadedAt = a.UploadedAt
                }).ToList()
            }).ToList();

        return new ColumnResponse
        {
            Id = column.Id,
            Name = column.Name,
            SortOrder = column.SortOrder,
            Tasks = tasks
        };
    }
}
