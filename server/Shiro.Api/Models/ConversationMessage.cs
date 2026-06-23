namespace Shiro.Api.Models;

public sealed class ConversationMessage
{
    public required string Id { get; init; }

    public required string Role { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
