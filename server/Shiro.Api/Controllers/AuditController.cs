using Microsoft.AspNetCore.Mvc;
using Shiro.Api.Models;
using Shiro.Api.Services;

namespace Shiro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogService auditLogService;

    public AuditController(IAuditLogService auditLogService)
    {
        this.auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<AuditLogEntry>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<AuditLogEntry>> GetAuditLog()
    {
        return Ok(auditLogService.GetAll());
    }

    [HttpGet("approvals/{approvalId}")]
    [ProducesResponseType<IReadOnlyCollection<AuditLogEntry>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<AuditLogEntry>> GetApprovalAuditLog(string approvalId)
    {
        return Ok(auditLogService.GetByApprovalId(approvalId));
    }
}
