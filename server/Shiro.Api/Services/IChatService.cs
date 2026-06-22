using Shiro.Api.Models;

namespace Shiro.Api.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<ChatStreamEvent> StreamMessageAsync(ChatRequest request, CancellationToken cancellationToken);
}
