using System.Collections.Concurrent;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class InMemoryTaskService : ITaskService
{
    private readonly ConcurrentDictionary<string, ShiroTask> tasks = new();

    public ShiroTask CreateTask(string title)
    {
        var task = new ShiroTask
        {
            Id = Guid.NewGuid().ToString(),
            Title = title.Trim(),
            IsCompleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        tasks[task.Id] = task;

        return task;
    }

    public IReadOnlyCollection<ShiroTask> GetTasks()
    {
        return tasks.Values
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToArray();
    }
}
