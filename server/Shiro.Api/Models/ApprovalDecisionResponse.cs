namespace Shiro.Api.Models;

public sealed class ApprovalDecisionResponse
{
    public required string ApprovalId { get; init; }

    public ApprovalStatus Status { get; init; }

    public required string Message { get; init; }

    public ToolExecutionResult? ExecutionResult { get; init; }
}
