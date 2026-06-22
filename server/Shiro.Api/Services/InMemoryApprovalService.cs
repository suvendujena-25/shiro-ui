using System.Collections.Concurrent;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class InMemoryApprovalService : IApprovalService
{
    private readonly ConcurrentDictionary<string, ApprovalRecord> approvals = new();

    public ApprovalRecord CreatePendingApproval(string conversationId, ToolRequest toolRequest)
    {
        var approval = new ApprovalRecord
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            ToolRequest = toolRequest,
            Status = ApprovalStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        approvals[approval.Id] = approval;

        return approval;
    }

    public IReadOnlyCollection<ApprovalRecord> GetPendingApprovals()
    {
        return approvals.Values
            .Where(approval => approval.Status == ApprovalStatus.Pending)
            .OrderByDescending(approval => approval.CreatedAtUtc)
            .ToArray();
    }

    public ApprovalRecord? GetApproval(string approvalId)
    {
        return approvals.GetValueOrDefault(approvalId);
    }

    public ApprovalRecord? Approve(string approvalId)
    {
        return Decide(approvalId, ApprovalStatus.Approved);
    }

    public ApprovalRecord? Reject(string approvalId)
    {
        return Decide(approvalId, ApprovalStatus.Rejected);
    }

    public ApprovalRecord? MarkExecuted(string approvalId, ToolExecutionResult executionResult)
    {
        if (!approvals.TryGetValue(approvalId, out var approval))
        {
            return null;
        }

        approval.Status = executionResult.Succeeded
            ? ApprovalStatus.Executed
            : ApprovalStatus.Failed;
        approval.ExecutedAtUtc = executionResult.ExecutedAtUtc;
        approval.ExecutionResult = executionResult;

        return approval;
    }

    private ApprovalRecord? Decide(string approvalId, ApprovalStatus status)
    {
        if (!approvals.TryGetValue(approvalId, out var approval))
        {
            return null;
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            return approval;
        }

        approval.Status = status;
        approval.DecidedAtUtc = DateTimeOffset.UtcNow;

        return approval;
    }
}
