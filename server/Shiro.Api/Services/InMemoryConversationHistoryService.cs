using System.Collections.Concurrent;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class InMemoryConversationHistoryService : IConversationHistoryService
{
    private readonly ConcurrentDictionary<string, List<ChatHistoryMessage>> conversations = new();
    private readonly object syncRoot = new();

    public IReadOnlyCollection<ChatHistoryMessage> GetRecentMessages(string conversationId, int maxMessages)
    {
        if (!conversations.TryGetValue(conversationId, out var messages))
        {
            return [];
        }

        lock (syncRoot)
        {
            return messages
                .TakeLast(maxMessages)
                .ToArray();
        }
    }

    public IReadOnlyCollection<ConversationSummary> GetConversations(int maxConversations)
    {
        lock (syncRoot)
        {
            return conversations
                .Select(conversation =>
                {
                    var messages = conversation.Value;
                    var firstMessage = messages.FirstOrDefault();
                    var lastMessage = messages.LastOrDefault();

                    return new ConversationSummary
                    {
                        Id = conversation.Key,
                        Title = BuildConversationTitle(firstMessage?.Content ?? "New conversation"),
                        MessageCount = messages.Count,
                        CreatedAtUtc = firstMessage?.CreatedAtUtc ?? DateTimeOffset.UtcNow,
                        UpdatedAtUtc = lastMessage?.CreatedAtUtc ?? DateTimeOffset.UtcNow
                    };
                })
                .OrderByDescending(conversation => conversation.UpdatedAtUtc)
                .Take(maxConversations)
                .ToArray();
        }
    }

    public IReadOnlyCollection<ConversationMessage> GetConversationMessages(string conversationId)
    {
        if (!conversations.TryGetValue(conversationId, out var messages))
        {
            return [];
        }

        lock (syncRoot)
        {
            return messages
                .Select(message => new ConversationMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = message.Role,
                    Content = message.Content,
                    CreatedAtUtc = message.CreatedAtUtc
                })
                .ToArray();
        }
    }

    public void AddUserMessage(string conversationId, string message)
    {
        AddMessage(conversationId, "user", message);
    }

    public void AddAssistantMessage(string conversationId, string message)
    {
        AddMessage(conversationId, "assistant", message);
    }

    private void AddMessage(string conversationId, string role, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var messages = conversations.GetOrAdd(conversationId, _ => []);

        lock (syncRoot)
        {
            messages.Add(new ChatHistoryMessage
            {
                Role = role,
                Content = message.Trim(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            if (messages.Count > 60)
            {
                messages.RemoveRange(0, messages.Count - 60);
            }
        }
    }

    private static string BuildConversationTitle(string message)
    {
        var title = message.Trim();

        if (title.Length <= 80)
        {
            return title;
        }

        return $"{title[..77]}...";
    }
}
