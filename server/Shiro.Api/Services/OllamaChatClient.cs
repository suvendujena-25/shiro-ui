using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Shiro.Api.Models;

namespace Shiro.Api.Services;

public sealed class OllamaChatClient : IAiChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly OllamaOptions options;

    public OllamaChatClient(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async Task<string?> GetReplyAsync(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        CancellationToken cancellationToken)
    {
        using var request = BuildChatRequest(userMessage, history, stream: false);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BuildOllamaErrorMessage(responseJson);
            }

            using var document = JsonDocument.Parse(responseJson);

            if (document.RootElement.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }

            return "I reached Ollama, but the response did not include a message.";
        }
        catch (HttpRequestException)
        {
            return "I could not reach Ollama at http://localhost:11434. Start Ollama, pull a model, then restart Shiro.Api.";
        }
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = BuildChatRequest(userMessage, history, stream: true);

        HttpResponseMessage? response;

        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            response = null;
        }

        if (response is null)
        {
            yield return "I could not reach Ollama at http://localhost:11434. Start Ollama, pull a model, then restart Shiro.Api.";
            yield break;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                yield return BuildOllamaErrorMessage(responseJson);
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var chunk = TryReadStreamChunk(line);

                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }
    }

    private HttpRequestMessage BuildChatRequest(
        string userMessage,
        IReadOnlyCollection<ChatHistoryMessage> history,
        bool stream)
    {
        var messages = new List<OllamaMessage>
        {
            new()
            {
                Role = "system",
                Content = BuildSystemInstruction()
            }
        };

        messages.AddRange(history.Select(message => new OllamaMessage
        {
            Role = message.Role,
            Content = message.Content
        }));

        messages.Add(new OllamaMessage
        {
            Role = "user",
            Content = userMessage
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
        request.Content = JsonContent(new OllamaChatRequest
        {
            Model = options.Model,
            Stream = stream,
            KeepAlive = options.KeepAlive,
            Options = new OllamaRequestOptions
            {
                NumPredict = options.MaxTokens,
                Temperature = 0.4
            },
            Messages = messages
        });

        return request;
    }

    private static StringContent JsonContent<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value, JsonOptions),
            Encoding.UTF8,
            "application/json");
    }

    private static string BuildSystemInstruction()
    {
        return """
            You are Shiro, a careful personal AI assistant inside a learning project.
            Be conversational, concise, and useful. Prefer short replies of 4 to 8 sentences unless the user asks for detail.
            Use the recent conversation history to remember context inside the current chat.
            Current backend tools:
            - If the user wants a local task, tell them to say: create task <title>.
            - If the user asks to send email, call someone, message someone, delete files, pay bills, or take another risky action, explain that Shiro must ask for approval first and must not claim the action is done.
            Never say you called, emailed, messaged, deleted files, or paid anything.
            """;
    }

    private static string BuildOllamaErrorMessage(string responseJson)
    {
        var message = TryReadOllamaError(responseJson);

        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return $"Ollama is running, but the configured model is not downloaded. Ollama said: {message}";
        }

        return $"I reached Ollama, but the local model request failed. Ollama said: {message}";
    }

    private static string TryReadOllamaError(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return "No error details were returned.";
        }

        try
        {
            using var document = JsonDocument.Parse(responseJson);

            if (document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString() ?? "No error message was returned.";
            }
        }
        catch (JsonException)
        {
            return "The error response was not valid JSON.";
        }

        return "No error message was returned.";
    }

    private static string? TryReadStreamChunk(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);

            if (document.RootElement.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required IReadOnlyCollection<OllamaMessage> Messages { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("keep_alive")]
        public string? KeepAlive { get; init; }

        [JsonPropertyName("options")]
        public OllamaRequestOptions? Options { get; init; }
    }

    private sealed class OllamaRequestOptions
    {
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }
    }
}
