namespace Shiro.Api.Models;

public sealed class TaskResponse
{
    public required ShiroTask Task { get; init; }

    public required ToolExecutionResult ExecutionResult { get; init; }
}
