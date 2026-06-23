using Microsoft.EntityFrameworkCore;
using Shiro.Api.Data;
using Shiro.Api.Data.Entities;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class DatabaseConversationHistoryService : IConversationHistoryService
{
    private const string LocalDevelopmentUserId = "local-dev-user";

    private readonly ShiroDbContext dbContext;

    public DatabaseConversationHistoryService(ShiroDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IReadOnlyCollection<ChatHistoryMessage> GetRecentMessages(string conversationId, int maxMessages)
    {
        return dbContext.ConversationMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAtUtc)
            .Take(maxMessages)
            .OrderBy(message => message.CreatedAtUtc)
            .Select(message => new ChatHistoryMessage
            {
                Role = message.Role,
                Content = message.Content,
                CreatedAtUtc = ToDateTimeOffset(message.CreatedAtUtc)
            })
            .ToArray();
    }

    public IReadOnlyCollection<ConversationSummary> GetConversations(int maxConversations)
    {
        return dbContext.Conversations
            .AsNoTracking()
            .OrderByDescending(conversation => conversation.UpdatedAtUtc)
            .Take(maxConversations)
            .Select(conversation => new ConversationSummary
            {
                Id = conversation.Id,
                Title = conversation.Title,
                MessageCount = conversation.Messages.Count,
                CreatedAtUtc = ToDateTimeOffset(conversation.CreatedAtUtc),
                UpdatedAtUtc = ToDateTimeOffset(conversation.UpdatedAtUtc)
            })
            .ToArray();
    }

    public IReadOnlyCollection<ConversationMessage> GetConversationMessages(string conversationId)
    {
        return dbContext.ConversationMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAtUtc)
            .Select(message => new ConversationMessage
            {
                Id = message.Id,
                Role = message.Role,
                Content = message.Content,
                CreatedAtUtc = ToDateTimeOffset(message.CreatedAtUtc)
            })
            .ToArray();
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

        var conversation = dbContext.Conversations.Find(conversationId);
        var now = DateTime.UtcNow;

        if (conversation is null)
        {
            conversation = new ConversationRecord
            {
                Id = conversationId,
                OwnerUserId = LocalDevelopmentUserId,
                Title = BuildConversationTitle(message),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            dbContext.Conversations.Add(conversation);
        }
        else
        {
            conversation.UpdatedAtUtc = now;
        }

        dbContext.ConversationMessages.Add(new ConversationMessageRecord
        {
            ConversationId = conversationId,
            Role = role,
            Content = message.Trim(),
            CreatedAtUtc = now
        });

        dbContext.SaveChanges();
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

    private static DateTimeOffset ToDateTimeOffset(DateTime utcDateTime)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
    }
}
