namespace Shiro.Api.Models;

public sealed class OpenAiOptions
{
    public string BaseUrl { get; init; } = "https://api.openai.com";

    public string Model { get; init; } = "gpt-4.1-mini";

    public string? ApiKey { get; init; }
}
