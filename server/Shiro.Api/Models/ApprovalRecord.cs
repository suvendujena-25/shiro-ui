namespace Shiro.Api.Models;

public sealed class ApprovalRecord
{
    public required string Id { get; init; }

    public required string ConversationId { get; init; }

    public required ToolRequest ToolRequest { get; init; }

    public ApprovalStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? DecidedAtUtc { get; set; }

    public DateTimeOffset? ExecutedAtUtc { get; set; }

    public ToolExecutionResult? ExecutionResult { get; set; }
}
