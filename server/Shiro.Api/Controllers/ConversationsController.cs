using Microsoft.AspNetCore.Mvc;
using Shiro.Api.Models;
using Shiro.Api.Services;

namespace Shiro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConversationsController : ControllerBase
{
    private readonly IConversationHistoryService conversationHistoryService;

    public ConversationsController(IConversationHistoryService conversationHistoryService)
    {
        this.conversationHistoryService = conversationHistoryService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ConversationSummary>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<ConversationSummary>> GetConversations()
    {
        return Ok(conversationHistoryService.GetConversations(30));
    }

    [HttpGet("{conversationId}/messages")]
    [ProducesResponseType<IReadOnlyCollection<ConversationMessage>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<ConversationMessage>> GetConversationMessages(
        string conversationId)
    {
        return Ok(conversationHistoryService.GetConversationMessages(conversationId));
    }
}
