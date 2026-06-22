using System.Collections.Concurrent;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class InMemoryAuditLogService : IAuditLogService
{
    private readonly ConcurrentQueue<AuditLogEntry> entries = new();

    public AuditLogEntry Record(
        AuditEventType eventType,
        ApprovalRecord approval,
        string message)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = eventType,
            ApprovalId = approval.Id,
            ConversationId = approval.ConversationId,
            ToolName = approval.ToolRequest.ToolName,
            Message = message,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        entries.Enqueue(entry);

        return entry;
    }

    public AuditLogEntry RecordSafeToolExecution(
        string conversationId,
        ToolRequest toolRequest,
        string message)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = AuditEventType.SafeToolExecuted,
            ApprovalId = null,
            ConversationId = conversationId,
            ToolName = toolRequest.ToolName,
            Message = message,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        entries.Enqueue(entry);

        return entry;
    }

    public IReadOnlyCollection<AuditLogEntry> GetAll()
    {
        return entries
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .ToArray();
    }

    public IReadOnlyCollection<AuditLogEntry> GetByApprovalId(string approvalId)
    {
        return entries
            .Where(entry => entry.ApprovalId == approvalId)
            .OrderBy(entry => entry.CreatedAtUtc)
            .ToArray();
    }
}
