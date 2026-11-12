using System.Net;
using System.Net.Http.Json;
using ToDoApp.Application.DTOs;

namespace ToDoApp.Tests;

public class TasksControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public TasksControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // GET /api/tasks — returns 200 with an array
    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        Assert.NotNull(body);
    }

    // POST /api/tasks — valid title creates task and returns 201
    [Fact]
    public async Task Create_WithValidTitle_Returns201AndTask()
    {
        var request = new CreateTaskRequest("Buy groceries");
        var response = await _client.PostAsJsonAsync("/api/tasks", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(dto);
        Assert.Equal("Buy groceries", dto!.Title);
        Assert.False(dto.IsCompleted);
    }

    // POST /api/tasks — empty title returns 400
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithEmptyTitle_Returns400(string title)
    {
        var request = new { Title = title };
        var response = await _client.PostAsJsonAsync("/api/tasks", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // POST /api/tasks — missing title field returns 400
    [Fact]
    public async Task Create_WithMissingTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // PATCH /api/tasks/{id}/complete — marks task completed
    [Fact]
    public async Task Complete_ExistingTask_Returns200AndIsCompleted()
    {
        // arrange: create a task
        var created = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Task to complete"));
        var dto = await created.Content.ReadFromJsonAsync<TaskDto>();

        // act
        var response = await _client.PatchAsync($"/api/tasks/{dto!.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TaskDto>();
        Assert.True(updated!.IsCompleted);
        Assert.NotNull(updated.CompletedAt);
    }

    // PATCH /api/tasks/{id}/complete — unknown id returns 404
    [Fact]
    public async Task Complete_UnknownId_Returns404()
    {
        var response = await _client.PatchAsync($"/api/tasks/{Guid.NewGuid()}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/tasks/{id} — deletes task and returns 204
    [Fact]
    public async Task Delete_ExistingTask_Returns204()
    {
        var created = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Task to delete"));
        var dto = await created.Content.ReadFromJsonAsync<TaskDto>();

        var response = await _client.DeleteAsync($"/api/tasks/{dto!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // DELETE /api/tasks/{id} — unknown id returns 404
    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // GET /api/tasks/{id} — task appears in list after creation
    [Fact]
    public async Task GetAll_AfterCreation_ContainsNewTask()
    {
        var title = $"Listed task {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title));

        var response = await _client.GetAsync("/api/tasks");
        var list = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        Assert.Contains(list!, t => t.Title == title);
    }

    // GET /api/health — smoke check
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
