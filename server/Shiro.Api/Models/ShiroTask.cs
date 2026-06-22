namespace Shiro.Api.Models;

public sealed class ShiroTask
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
