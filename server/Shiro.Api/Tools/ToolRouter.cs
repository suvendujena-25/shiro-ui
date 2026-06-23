using System.Text.RegularExpressions;
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

        if (TryCreateWeatherLookupToolRequest(userMessage, out var weatherToolRequest))
        {
            return ToolRouteResult.FromToolRequest(weatherToolRequest);
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

    private static bool TryCreateWeatherLookupToolRequest(string userMessage, out ToolRequest toolRequest)
    {
        if (IsWeatherQuestion(userMessage))
        {
            toolRequest = new ToolRequest
            {
                ToolName = ToolNames.WeatherLookup,
                RequiresApproval = false,
                Reason = "Checking weather is a safe read-only lookup, so Shiro can do it immediately.",
                Arguments =
                {
                    ["rawUserMessage"] = userMessage
                }
            };

            var location = ExtractLocation(userMessage);

            if (!string.IsNullOrWhiteSpace(location))
            {
                toolRequest.Arguments["location"] = location;
            }

            return true;
        }

        toolRequest = null!;
        return false;
    }

    private static bool IsWeatherQuestion(string userMessage)
    {
        return ContainsAny(userMessage, WeatherWords);
    }

    private static string? ExtractLocation(string userMessage)
    {
        var markers = new[]
        {
            " in ",
            " for ",
            " at "
        };

        foreach (var marker in markers)
        {
            var markerIndex = userMessage.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 0)
            {
                continue;
            }

            var location = userMessage[(markerIndex + marker.Length)..]
                .Trim(' ', '?', '.', '!', ',');

            return string.IsNullOrWhiteSpace(location) ? null : location;
        }

        return null;
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
        var message = userMessage.Trim();

        return ContainsAny(message, DateTimeQuestionPhrases)
            || (ContainsAnyWholeWord(message, TimeWords) && ContainsAnyWholeWord(message, QuestionWords));
    }

    private static bool ContainsAny(string value, IReadOnlyCollection<string> phrases)
    {
        return phrases.Any(phrase => value.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAnyWholeWord(string value, IReadOnlyCollection<string> words)
    {
        return words.Any(word => Regex.IsMatch(
            value,
            $@"\b{Regex.Escape(word)}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static readonly string[] DateTimeQuestionPhrases =
    [
        "current date",
        "current time",
        "date today",
        "today's date",
        "todays date",
        "what date",
        "what day is it",
        "which day is it",
        "time now",
        "date and time",
        "time and date"
    ];

    private static readonly string[] TimeWords =
    [
        "time"
    ];

    private static readonly string[] WeatherWords =
    [
        "weather",
        "rain",
        "raining",
        "temperature",
        "forecast"
    ];

    private static readonly string[] QuestionWords =
    [
        "what",
        "which",
        "current",
        "now"
    ];

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
