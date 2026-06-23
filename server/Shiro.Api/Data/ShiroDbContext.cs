using Microsoft.EntityFrameworkCore;
using Shiro.Api.Data.Entities;

namespace Shiro.Api.Data;

public sealed class ShiroDbContext : DbContext
{
    public ShiroDbContext(DbContextOptions<ShiroDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConversationRecord> Conversations => Set<ConversationRecord>();

    public DbSet<ConversationMessageRecord> ConversationMessages => Set<ConversationMessageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationRecord>(entity =>
        {
            entity.HasKey(conversation => conversation.Id);
            entity.Property(conversation => conversation.OwnerUserId).HasMaxLength(128);
            entity.Property(conversation => conversation.Title).HasMaxLength(160);
            entity.HasIndex(conversation => new { conversation.OwnerUserId, conversation.UpdatedAtUtc });

            entity
                .HasMany(conversation => conversation.Messages)
                .WithOne(message => message.Conversation)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationMessageRecord>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Role).HasMaxLength(32);
            entity.HasIndex(message => new { message.ConversationId, message.CreatedAtUtc });
        });
    }
}
