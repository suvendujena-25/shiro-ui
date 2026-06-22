using Microsoft.AspNetCore.Mvc;
using Shiro.Api.Models;
using Shiro.Api.Services;
using Shiro.Api.Tools;

namespace Shiro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService taskService;

    public TasksController(ITaskService taskService)
    {
        this.taskService = taskService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ShiroTask>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<ShiroTask>> GetTasks()
    {
        return Ok(taskService.GetTasks());
    }

    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<TaskResponse> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError(nameof(request.Title), "Title is required.");
            return ValidationProblem(ModelState);
        }

        var task = taskService.CreateTask(request.Title);

        return Ok(new TaskResponse
        {
            Task = task,
            ExecutionResult = new ToolExecutionResult
            {
                ToolName = ToolNames.CreateTask,
                Succeeded = true,
                Simulated = false,
                Message = $"Task created: {task.Title}",
                ExecutedAtUtc = task.CreatedAtUtc
            }
        });
    }
}
