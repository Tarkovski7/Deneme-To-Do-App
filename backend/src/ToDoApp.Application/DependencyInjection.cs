using Microsoft.Extensions.DependencyInjection;
using ToDoApp.Application.Services;

namespace ToDoApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<TaskService>();
        return services;
    }
}
