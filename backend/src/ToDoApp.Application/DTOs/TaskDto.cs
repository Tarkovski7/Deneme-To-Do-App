namespace ToDoApp.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt);
