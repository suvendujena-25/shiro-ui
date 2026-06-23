namespace Shiro.Api.Data.Entities;

public sealed class ConversationMessageRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string ConversationId { get; init; }

    public required string Role { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public ConversationRecord? Conversation { get; init; }
}
