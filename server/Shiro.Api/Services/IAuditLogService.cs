using Shiro.Api.Models;

namespace Shiro.Api.Services;

public interface IAuditLogService
{
    AuditLogEntry Record(
        AuditEventType eventType,
        ApprovalRecord approval,
        string message);

    AuditLogEntry RecordSafeToolExecution(
        string conversationId,
        ToolRequest toolRequest,
        string message);

    IReadOnlyCollection<AuditLogEntry> GetAll();

    IReadOnlyCollection<AuditLogEntry> GetByApprovalId(string approvalId);
}
