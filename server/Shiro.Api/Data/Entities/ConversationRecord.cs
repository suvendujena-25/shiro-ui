namespace Shiro.Api.Data.Entities;

public sealed class ConversationRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string OwnerUserId { get; set; } = "local-dev-user";

    public string Title { get; set; } = "New conversation";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ConversationMessageRecord> Messages { get; init; } = [];
}
