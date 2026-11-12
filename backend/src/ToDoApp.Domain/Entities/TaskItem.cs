namespace ToDoApp.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private TaskItem() { }

    public static TaskItem Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = null
        };
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }
}
