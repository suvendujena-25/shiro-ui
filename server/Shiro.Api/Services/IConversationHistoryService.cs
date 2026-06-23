using Shiro.Api.Models;

namespace Shiro.Api.Services;

public interface IConversationHistoryService
{
    IReadOnlyCollection<ChatHistoryMessage> GetRecentMessages(string conversationId, int maxMessages);

    IReadOnlyCollection<ConversationSummary> GetConversations(int maxConversations);

    IReadOnlyCollection<ConversationMessage> GetConversationMessages(string conversationId);

    void AddUserMessage(string conversationId, string message);

    void AddAssistantMessage(string conversationId, string message);
}
