using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Shiro.Api.Models;
using Shiro.Api.Services;

namespace Shiro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatService chatService;

    public ChatController(IChatService chatService)
    {
        this.chatService = chatService;
    }

    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            ModelState.AddModelError(nameof(request.Message), "Message is required.");
            return ValidationProblem(ModelState);
        }

        var response = await chatService.SendMessageAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task StreamMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = "Message is required." },
                cancellationToken);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await foreach (var streamEvent in chatService.StreamMessageAsync(request, cancellationToken))
        {
            await Response.WriteAsync($"event: {streamEvent.EventName}\n", cancellationToken);
            await Response.WriteAsync(
                $"data: {JsonSerializer.Serialize(streamEvent.Data, JsonOptions)}\n\n",
                cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
