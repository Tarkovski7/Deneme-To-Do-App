using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDoApp.Domain.Entities;

namespace ToDoApp.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.IsCompleted).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CompletedAt).IsRequired(false);
    }
}
