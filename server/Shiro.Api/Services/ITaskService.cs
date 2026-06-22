using Shiro.Api.Models;

namespace Shiro.Api.Services;

public interface ITaskService
{
    ShiroTask CreateTask(string title);

    IReadOnlyCollection<ShiroTask> GetTasks();
}
