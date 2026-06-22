namespace Shiro.Api.Models;

public sealed class ChatStreamEvent
{
    public required string EventName { get; init; }

    public required object Data { get; init; }
}
