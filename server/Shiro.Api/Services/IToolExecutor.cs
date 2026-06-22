using Shiro.Api.Models;

namespace Shiro.Api.Services;

public interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(ToolRequest toolRequest, CancellationToken cancellationToken);
}
