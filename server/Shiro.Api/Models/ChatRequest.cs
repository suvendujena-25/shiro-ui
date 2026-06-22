namespace Shiro.Api.Models;

public sealed class ChatRequest
{
    public string? ConversationId { get; init; }

    public required string Message { get; init; }
}
