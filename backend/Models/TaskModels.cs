using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class TaskItem
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime? Deadline { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsFavorite { get; set; }
    
    public int ColumnId { get; set; }
    
    public virtual TaskColumn Column { get; set; } = null!;
    
    public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    
    public int SortOrder { get; set; }
}

public class TaskColumn
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public int SortOrder { get; set; }
    
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public class TaskAttachment
{
    public int Id { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public int TaskId { get; set; }
    
    public virtual TaskItem Task { get; set; } = null!;
}
