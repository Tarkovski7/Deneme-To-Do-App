using System.ComponentModel.DataAnnotations;

namespace ToDoApp.Application.DTOs;

public record CreateTaskRequest(
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 500 characters.")]
    string Title);
