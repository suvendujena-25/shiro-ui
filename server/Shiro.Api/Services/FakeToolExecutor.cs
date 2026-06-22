using Shiro.Api.Models;
using Shiro.Api.Tools;

namespace Shiro.Api.Services;

public sealed class FakeToolExecutor : IToolExecutor
{
    public Task<ToolExecutionResult> ExecuteAsync(ToolRequest toolRequest, CancellationToken cancellationToken)
    {
        var result = new ToolExecutionResult
        {
            ToolName = toolRequest.ToolName,
            Succeeded = true,
            Simulated = true,
            Message = BuildSimulationMessage(toolRequest),
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult(result);
    }

    private static string BuildSimulationMessage(ToolRequest toolRequest)
    {
        return toolRequest.ToolName switch
        {
            ToolNames.SendEmail => "Simulated only: no email was sent. Shiro confirmed the approval flow works safely.",
            _ => $"Simulated only: {toolRequest.ToolName} was not executed against any real system."
        };
    }
}
