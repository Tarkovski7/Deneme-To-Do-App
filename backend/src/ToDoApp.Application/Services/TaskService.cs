using ToDoApp.Application.DTOs;
using ToDoApp.Application.Interfaces;
using ToDoApp.Domain.Entities;

namespace ToDoApp.Application.Services;

public class TaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList().AsReadOnly();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<TaskDto> CreateAsync(string title, CancellationToken cancellationToken = default)
    {
        var task = TaskItem.Create(title);
        await _repository.AddAsync(task, cancellationToken);
        return Map(task);
    }

    public async Task<TaskDto?> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(id, cancellationToken);
        if (task is null) return null;
        task.MarkCompleted();
        await _repository.UpdateAsync(task, cancellationToken);
        return Map(task);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(id, cancellationToken);
        if (task is null) return false;
        await _repository.DeleteAsync(task, cancellationToken);
        return true;
    }

    private static TaskDto Map(TaskItem t) =>
        new(t.Id, t.Title, t.IsCompleted, t.CreatedAt, t.CompletedAt);
}
