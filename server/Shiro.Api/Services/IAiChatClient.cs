namespace Shiro.Api.Services;

public interface IAiChatClient
{
    Task<string?> GetReplyAsync(string userMessage, CancellationToken cancellationToken);

    IAsyncEnumerable<string> StreamReplyAsync(string userMessage, CancellationToken cancellationToken);
}
