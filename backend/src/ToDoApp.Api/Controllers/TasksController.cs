using Microsoft.AspNetCore.Mvc;
using ToDoApp.Application.DTOs;
using ToDoApp.Application.Services;

namespace ToDoApp.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(cancellationToken);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Guard against whitespace-only titles that pass DataAnnotations
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required." });

        var dto = await _taskService.CreateAsync(request.Title, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _taskService.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<TaskDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _taskService.CompleteAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _taskService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
