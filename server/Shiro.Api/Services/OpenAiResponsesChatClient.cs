using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class OpenAiResponsesChatClient : IOpenAiChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly OpenAiOptions options;

    public OpenAiResponsesChatClient(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async Task<string?> GetReplyAsync(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var input = new List<OpenAiInputMessage>
        {
            new()
            {
                Role = "developer",
                Content =
                [
                    new OpenAiInputContent
                    {
                        Text = BuildDeveloperInstruction(),
                        Type = "input_text"
                    }
                ]
            }
        };

        input.AddRange(history.Select(message => new OpenAiInputMessage
        {
            Role = message.Role,
            Content =
            [
                new OpenAiInputContent
                {
                    Text = message.Content,
                    Type = "input_text"
                }
            ]
        }));

        input.Add(new OpenAiInputMessage
        {
            Role = "user",
            Content =
            [
                new OpenAiInputContent
                {
                    Text = userMessage,
                    Type = "input_text"
                }
            ]
        });

        request.Content = JsonContent(new OpenAiResponseRequest
        {
            Model = options.Model,
            Input = input
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return BuildOpenAiErrorMessage(response.StatusCode, responseJson);
        }

        using var document = JsonDocument.Parse(responseJson);

        return TryReadOutputText(document.RootElement);
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reply = await GetReplyAsync(userMessage, history, cancellationToken);

        if (!string.IsNullOrWhiteSpace(reply))
        {
            yield return reply;
        }
    }

    private string? GetApiKey()
    {
        return !string.IsNullOrWhiteSpace(options.ApiKey)
            ? options.ApiKey
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private static StringContent JsonContent<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value, JsonOptions),
            Encoding.UTF8,
            "application/json");
    }

    private static string BuildDeveloperInstruction()
    {
        return """
            You are Shiro, a careful personal AI assistant inside a learning project.
            Be conversational, concise, and useful.
            Use the recent conversation history to remember context inside the current chat.
            Current backend tools:
            - If the user wants a local task, tell them to say: create task <title>.
            - If the user asks to send email, message someone, delete files, pay bills, or take another risky action, explain that Shiro must ask for approval first and must not claim the action is done.
            Never say you sent an email, messaged someone, deleted files, or paid anything.
            """;
    }

    private static string BuildOpenAiErrorMessage(System.Net.HttpStatusCode statusCode, string responseJson)
    {
        var message = TryReadOpenAiErrorMessage(responseJson);

        return statusCode switch
        {
            System.Net.HttpStatusCode.TooManyRequests =>
                $"I reached OpenAI, but the account is currently rate-limited or out of quota. OpenAI said: {message}",
            System.Net.HttpStatusCode.Unauthorized =>
                $"I reached OpenAI, but the API key was rejected. OpenAI said: {message}",
            System.Net.HttpStatusCode.Forbidden =>
                $"I reached OpenAI, but this key does not have permission for the request. OpenAI said: {message}",
            System.Net.HttpStatusCode.BadRequest =>
                $"I reached OpenAI, but the request was not accepted. OpenAI said: {message}",
            _ =>
                $"I reached OpenAI, but the request failed with HTTP {(int)statusCode}. OpenAI said: {message}"
        };
    }

    private static string TryReadOpenAiErrorMessage(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return "No error details were returned.";
        }

        try
        {
            using var document = JsonDocument.Parse(responseJson);

            if (document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? "No error message was returned.";
            }
        }
        catch (JsonException)
        {
            return "The error response was not valid JSON.";
        }

        return "No error message was returned.";
    }

    private static string? TryReadOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText)
            && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output)
            || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text)
                    && text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private sealed class OpenAiResponseRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("input")]
        public required IReadOnlyCollection<OpenAiInputMessage> Input { get; init; }
    }

    private sealed class OpenAiInputMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required IReadOnlyCollection<OpenAiInputContent> Content { get; init; }
    }

    private sealed class OpenAiInputContent
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("text")]
        public required string Text { get; init; }
    }
}
