using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime? Deadline { get; set; }
    
    public int ColumnId { get; set; }
}

public class UpdateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime? Deadline { get; set; }
    
    public bool IsFavorite { get; set; }
    
    public int ColumnId { get; set; }
}

public class MoveTaskRequest
{
    public int ColumnId { get; set; }
    public int SortOrder { get; set; }
}

public class TaskResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsFavorite { get; set; }
    public int ColumnId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<AttachmentResponse> Attachments { get; set; } = new();
}

public class AttachmentResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CreateColumnRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class ColumnResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<TaskResponse> Tasks { get; set; } = new();
}

public class BoardResponse
{
    public List<ColumnResponse> Columns { get; set; } = new();
}
