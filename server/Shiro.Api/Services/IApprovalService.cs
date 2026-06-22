using Shiro.Api.Models;

namespace Shiro.Api.Services;

public interface IApprovalService
{
    ApprovalRecord CreatePendingApproval(string conversationId, ToolRequest toolRequest);

    IReadOnlyCollection<ApprovalRecord> GetPendingApprovals();

    ApprovalRecord? GetApproval(string approvalId);

    ApprovalRecord? Approve(string approvalId);

    ApprovalRecord? Reject(string approvalId);

    ApprovalRecord? MarkExecuted(string approvalId, ToolExecutionResult executionResult);
}
