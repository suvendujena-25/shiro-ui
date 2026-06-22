namespace Shiro.Api.Models;

public sealed class AuditLogEntry
{
    public required string Id { get; init; }

    public required AuditEventType EventType { get; init; }

    public string? ApprovalId { get; init; }

    public required string ConversationId { get; init; }

    public required string ToolName { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
