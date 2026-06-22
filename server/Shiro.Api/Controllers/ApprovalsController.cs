using Microsoft.AspNetCore.Mvc;
using Shiro.Api.Models;
using Shiro.Api.Services;

namespace Shiro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalService approvalService;
    private readonly IToolExecutor toolExecutor;
    private readonly IAuditLogService auditLogService;

    public ApprovalsController(
        IApprovalService approvalService,
        IToolExecutor toolExecutor,
        IAuditLogService auditLogService)
    {
        this.approvalService = approvalService;
        this.toolExecutor = toolExecutor;
        this.auditLogService = auditLogService;
    }

    [HttpGet("pending")]
    [ProducesResponseType<IReadOnlyCollection<ApprovalRecord>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<ApprovalRecord>> GetPendingApprovals()
    {
        return Ok(approvalService.GetPendingApprovals());
    }

    [HttpGet("{approvalId}")]
    [ProducesResponseType<ApprovalRecord>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApprovalRecord> GetApproval(string approvalId)
    {
        var approval = approvalService.GetApproval(approvalId);

        return approval is null ? NotFound() : Ok(approval);
    }

    [HttpPost("{approvalId}/approve")]
    [ProducesResponseType<ApprovalDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalDecisionResponse>> Approve(
        string approvalId,
        CancellationToken cancellationToken)
    {
        var approval = approvalService.Approve(approvalId);

        if (approval is null)
        {
            return NotFound();
        }

        if (approval.Status == ApprovalStatus.Executed)
        {
            return Ok(new ApprovalDecisionResponse
            {
                ApprovalId = approval.Id,
                Status = approval.Status,
                Message = "This approval was already executed.",
                ExecutionResult = approval.ExecutionResult
            });
        }

        if (approval.Status != ApprovalStatus.Approved)
        {
            return Ok(new ApprovalDecisionResponse
            {
                ApprovalId = approval.Id,
                Status = approval.Status,
                Message = "This approval is no longer pending, so Shiro did not execute it.",
                ExecutionResult = approval.ExecutionResult
            });
        }

        auditLogService.Record(
            AuditEventType.ApprovalAccepted,
            approval,
            "User approved the risky tool request.");

        var executionResult = await toolExecutor.ExecuteAsync(approval.ToolRequest, cancellationToken);
        approval = approvalService.MarkExecuted(approval.Id, executionResult) ?? approval;
        auditLogService.Record(
            executionResult.Succeeded
                ? AuditEventType.ToolExecutionSucceeded
                : AuditEventType.ToolExecutionFailed,
            approval,
            executionResult.Message);

        return Ok(new ApprovalDecisionResponse
        {
            ApprovalId = approval.Id,
            Status = approval.Status,
            Message = "Approval accepted. Shiro ran the fake tool executor only; no real external action was performed.",
            ExecutionResult = approval.ExecutionResult
        });
    }

    [HttpPost("{approvalId}/reject")]
    [ProducesResponseType<ApprovalDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApprovalDecisionResponse> Reject(string approvalId)
    {
        var existingApproval = approvalService.GetApproval(approvalId);
        var wasPending = existingApproval?.Status == ApprovalStatus.Pending;
        var approval = approvalService.Reject(approvalId);

        if (approval is null)
        {
            return NotFound();
        }

        if (wasPending && approval.Status == ApprovalStatus.Rejected)
        {
            auditLogService.Record(
                AuditEventType.ApprovalRejected,
                approval,
                "User rejected the risky tool request.");
        }

        return Ok(new ApprovalDecisionResponse
        {
            ApprovalId = approval.Id,
            Status = approval.Status,
            Message = approval.Status == ApprovalStatus.Rejected
                ? "Approval rejected. Shiro will not execute this tool request."
                : "This approval was already decided, so Shiro did not change it.",
            ExecutionResult = approval.ExecutionResult
        });
    }
}
