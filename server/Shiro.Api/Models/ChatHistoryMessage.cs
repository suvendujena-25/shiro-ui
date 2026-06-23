namespace Shiro.Api.Models;

public sealed class ChatHistoryMessage
{
    public required string Role { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
