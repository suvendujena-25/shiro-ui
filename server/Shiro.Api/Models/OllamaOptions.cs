namespace Shiro.Api.Models;

public sealed class OllamaOptions
{
    public string BaseUrl { get; init; } = "http://localhost:11434";

    public string Model { get; init; } = "llama3.2:3b";

    public int MaxTokens { get; init; } = 180;

    public string KeepAlive { get; init; } = "15m";
}
