using Backend.Models;
using Backend.DTOs;

namespace Backend.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskResponse>> GetAllTasksAsync();
    Task<TaskResponse?> GetTaskByIdAsync(int id);
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request);
    Task<TaskResponse?> UpdateTaskAsync(int id, UpdateTaskRequest request);
    Task<bool> DeleteTaskAsync(int id);
    Task<TaskResponse?> MoveTaskAsync(int id, MoveTaskRequest request);
    Task<IEnumerable<TaskResponse>> GetTasksByColumnAsync(int columnId);
}

public interface IColumnService
{
    Task<IEnumerable<ColumnResponse>> GetAllColumnsAsync();
    Task<ColumnResponse?> GetColumnByIdAsync(int id);
    Task<ColumnResponse> CreateColumnAsync(CreateColumnRequest request);
    Task<bool> DeleteColumnAsync(int id);
    Task<BoardResponse> GetBoardAsync();
}

public interface IAttachmentService
{
    Task<AttachmentResponse> UploadAttachmentAsync(int taskId, IFormFile file);
    Task<bool> DeleteAttachmentAsync(int attachmentId);
    Task<(Stream stream, string contentType, string fileName)?> DownloadAttachmentAsync(int attachmentId);
}
