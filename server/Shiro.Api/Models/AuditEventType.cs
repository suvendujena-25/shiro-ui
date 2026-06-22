namespace Shiro.Api.Models;

public enum AuditEventType
{
    ApprovalRequested = 1,
    ApprovalAccepted = 2,
    ApprovalRejected = 3,
    ToolExecutionSucceeded = 4,
    ToolExecutionFailed = 5,
    SafeToolExecuted = 6
}
