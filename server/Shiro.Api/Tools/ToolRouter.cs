using Shiro.Api.Models;

namespace Shiro.Api.Tools;

public sealed class ToolRouter : IToolRouter
{
    public ToolRouteResult Route(string userMessage)
    {
        if (TryCreateRiskyToolRequest(userMessage, out var riskyToolRequest))
        {
            return ToolRouteResult.FromToolRequest(riskyToolRequest);
        }

        if (TryCreateSafeTaskToolRequest(userMessage, out var safeToolRequest))
        {
            return ToolRouteResult.FromToolRequest(safeToolRequest);
        }

        if (TryCreateCurrentDateTimeToolRequest(userMessage, out var dateTimeToolRequest))
        {
            return ToolRouteResult.FromToolRequest(dateTimeToolRequest);
        }

        return ToolRouteResult.NoTool();
    }

    private static bool TryCreateRiskyToolRequest(string userMessage, out ToolRequest toolRequest)
    {
        if (userMessage.Contains("send email", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("send an email", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("email ", StringComparison.OrdinalIgnoreCase))
        {
            toolRequest = new ToolRequest
            {
                ToolName = ToolNames.SendEmail,
                RequiresApproval = true,
                Reason = "Sending email can message another person, so Shiro must ask for confirmation first.",
                Arguments =
                {
                    ["rawUserMessage"] = userMessage
                }
            };

            return true;
        }

        toolRequest = null!;
        return false;
    }

    private static bool TryCreateCurrentDateTimeToolRequest(string userMessage, out ToolRequest toolRequest)
    {
        if (IsCurrentDateTimeQuestion(userMessage))
        {
            toolRequest = new ToolRequest
            {
                ToolName = ToolNames.CurrentDateTime,
                RequiresApproval = false,
                Reason = "Reading the current local date and time is safe, so Shiro can answer immediately.",
                Arguments =
                {
                    ["rawUserMessage"] = userMessage
                }
            };

            return true;
        }

        toolRequest = null!;
        return false;
    }

    private static bool IsCurrentDateTimeQuestion(string userMessage)
    {
        return userMessage.Contains("today", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("current date", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("date today", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("what date", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("current time", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("what time", StringComparison.OrdinalIgnoreCase)
            || userMessage.Contains("time now", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateSafeTaskToolRequest(string userMessage, out ToolRequest toolRequest)
    {
        var title = ExtractTaskTitle(userMessage);

        if (!string.IsNullOrWhiteSpace(title))
        {
            toolRequest = new ToolRequest
            {
                ToolName = ToolNames.CreateTask,
                RequiresApproval = false,
                Reason = "Creating a local task is a safe action, so Shiro can do it immediately.",
                Arguments =
                {
                    ["title"] = title,
                    ["rawUserMessage"] = userMessage
                }
            };

            return true;
        }

        toolRequest = null!;
        return false;
    }

    private static string? ExtractTaskTitle(string userMessage)
    {
        var supportedPrefixes = new[]
        {
            "create task",
            "add task",
            "create a task",
            "add a task"
        };

        foreach (var prefix in supportedPrefixes)
        {
            if (userMessage.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return userMessage[prefix.Length..].Trim(' ', ':', '-', '.');
            }
        }

        return null;
    }
}
