using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskColumn> Columns { get; set; }
    public DbSet<TaskAttachment> Attachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Task entity
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.Column)
                  .WithMany(c => c.Tasks)
                  .HasForeignKey(e => e.ColumnId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Column entity
        modelBuilder.Entity<TaskColumn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Configure Attachment entity
        modelBuilder.Entity<TaskAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.FilePath).IsRequired();
            entity.HasOne(e => e.Task)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default columns
        modelBuilder.Entity<TaskColumn>().HasData(
            new TaskColumn { Id = 1, Name = "To Do", SortOrder = 1 },
            new TaskColumn { Id = 2, Name = "In Progress", SortOrder = 2 },
            new TaskColumn { Id = 3, Name = "Done", SortOrder = 3 }
        );
    }
}
