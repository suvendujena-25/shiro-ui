namespace Shiro.Api.Models;

public sealed class ConversationSummary
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public int MessageCount { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}
