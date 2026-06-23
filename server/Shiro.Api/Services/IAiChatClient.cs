namespace Shiro.Api.Services;

using Shiro.Api.Models;

public interface IAiChatClient
{
    Task<string?> GetReplyAsync(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        CancellationToken cancellationToken);

    IAsyncEnumerable<string> StreamReplyAsync(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        CancellationToken cancellationToken);
}
