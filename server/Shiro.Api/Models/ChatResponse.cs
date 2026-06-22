namespace Shiro.Api.Models;

public sealed class ChatResponse
{
    public required string ConversationId { get; init; }

    public ChatResponseType ResponseType { get; init; }

    public required string Message { get; init; }

    public bool RequiresApproval { get; init; }

    public string? ToolName { get; init; }

    public string? ApprovalId { get; init; }

    public ToolRequest? ToolRequest { get; init; }

    public ToolExecutionResult? ToolExecutionResult { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
