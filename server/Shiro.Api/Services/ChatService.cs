using Shiro.Api.Models;
using Shiro.Api.Tools;
using System.Text;

namespace Shiro.Api.Services;

public sealed class ChatService : IChatService
{
    private readonly IApprovalService approvalService;
    private readonly IAuditLogService auditLogService;
    private readonly ITaskService taskService;
    private readonly IDeviceInfoService deviceInfoService;
    private readonly IToolRouter toolRouter;
    private readonly IAiChatClient aiChatClient;

    public ChatService(
        IApprovalService approvalService,
        IAuditLogService auditLogService,
        ITaskService taskService,
        IDeviceInfoService deviceInfoService,
        IToolRouter toolRouter,
        IAiChatClient aiChatClient)
    {
        this.approvalService = approvalService;
        this.auditLogService = auditLogService;
        this.taskService = taskService;
        this.deviceInfoService = deviceInfoService;
        this.toolRouter = toolRouter;
        this.aiChatClient = aiChatClient;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? Guid.NewGuid().ToString()
            : request.ConversationId;
        var routeResult = toolRouter.Route(request.Message);

        if (routeResult.ToolRequest is { RequiresApproval: true } toolRequest)
        {
            var approval = approvalService.CreatePendingApproval(conversationId, toolRequest);
            auditLogService.Record(
                AuditEventType.ApprovalRequested,
                approval,
                "Risky tool request stored and waiting for user approval.");

            return new ChatResponse
            {
                ConversationId = conversationId,
                ResponseType = ChatResponseType.ApprovalRequired,
                Message = "I found a risky action. Please approve it before Shiro does anything.",
                RequiresApproval = true,
                ToolName = toolRequest.ToolName,
                ApprovalId = approval.Id,
                ToolRequest = toolRequest,
                ToolExecutionResult = null,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        if (routeResult.ToolRequest is { RequiresApproval: false } safeToolRequest)
        {
            var executionResult = ExecuteSafeTool(safeToolRequest);

            auditLogService.RecordSafeToolExecution(
                conversationId,
                safeToolRequest,
                executionResult.Message);

            return new ChatResponse
            {
                ConversationId = conversationId,
                ResponseType = ChatResponseType.ToolCall,
                Message = executionResult.Message,
                RequiresApproval = false,
                ToolName = safeToolRequest.ToolName,
                ApprovalId = null,
                ToolRequest = safeToolRequest,
                ToolExecutionResult = executionResult,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        var aiReply = await aiChatClient.GetReplyAsync(request.Message, cancellationToken);

        var response = new ChatResponse
        {
            ConversationId = conversationId,
            ResponseType = ChatResponseType.Message,
            Message = string.IsNullOrWhiteSpace(aiReply)
                ? BuildPlaceholderReply(request.Message)
                : aiReply,
            RequiresApproval = false,
            ToolName = null,
            ApprovalId = null,
            ToolRequest = null,
            ToolExecutionResult = null,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return response;
    }

    private ToolExecutionResult ExecuteSafeTool(ToolRequest safeToolRequest)
    {
        if (safeToolRequest.ToolName == ToolNames.CreateTask)
        {
            var task = taskService.CreateTask(safeToolRequest.Arguments["title"]);

            return new ToolExecutionResult
            {
                ToolName = safeToolRequest.ToolName,
                Succeeded = true,
                Simulated = false,
                Message = $"Task created: {task.Title}",
                ExecutedAtUtc = task.CreatedAtUtc
            };
        }

        if (safeToolRequest.ToolName == ToolNames.CurrentDateTime)
        {
            var executionResult = new ToolExecutionResult
            {
                ToolName = safeToolRequest.ToolName,
                Succeeded = true,
                Simulated = false,
                Message = deviceInfoService.GetCurrentDateTimeMessage(),
                ExecutedAtUtc = DateTimeOffset.UtcNow
            };

            return executionResult;
        }

        return new ToolExecutionResult
        {
            ToolName = safeToolRequest.ToolName,
            Succeeded = false,
            Simulated = false,
            Message = $"Safe tool '{safeToolRequest.ToolName}' is not implemented yet.",
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamMessageAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? Guid.NewGuid().ToString()
            : request.ConversationId;
        var routeResult = toolRouter.Route(request.Message);

        if (routeResult.ToolRequest is not null)
        {
            var response = await SendMessageAsync(
                new ChatRequest
                {
                    ConversationId = conversationId,
                    Message = request.Message
                },
                cancellationToken);

            yield return new ChatStreamEvent
            {
                EventName = "response",
                Data = response
            };
            yield break;
        }

        yield return new ChatStreamEvent
        {
            EventName = "meta",
            Data = new { conversationId }
        };

        var builder = new StringBuilder();

        await foreach (var chunk in aiChatClient.StreamReplyAsync(request.Message, cancellationToken))
        {
            builder.Append(chunk);
            yield return new ChatStreamEvent
            {
                EventName = "delta",
                Data = new { text = chunk }
            };
        }

        if (builder.Length == 0)
        {
            var fallbackReply = BuildPlaceholderReply(request.Message);
            builder.Append(fallbackReply);
            yield return new ChatStreamEvent
            {
                EventName = "delta",
                Data = new { text = fallbackReply }
            };
        }

        yield return new ChatStreamEvent
        {
            EventName = "done",
            Data = new ChatResponse
            {
                ConversationId = conversationId,
                ResponseType = ChatResponseType.Message,
                Message = builder.ToString(),
                RequiresApproval = false,
                ToolName = null,
                ApprovalId = null,
                ToolRequest = null,
                ToolExecutionResult = null,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }
        };
    }

    private static string BuildPlaceholderReply(string userMessage)
    {
        if (userMessage.Contains("hello", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("hi", StringComparison.OrdinalIgnoreCase))
        {
            return "Hello, I am Shiro. I can already create local tasks and prepare risky actions for approval. What would you like me to help with?";
        }

        if (userMessage.Contains("what can you do", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("help", StringComparison.OrdinalIgnoreCase))
        {
            return "Right now I can create tasks immediately when you say something like 'create task buy milk'. If you ask me to send an email, I will ask for approval first instead of doing it directly.";
        }

        return $"I heard you: \"{userMessage}\". I am not connected to the real AI model yet, but I can already route safe task requests and protect risky actions with approvals.";
    }
}
