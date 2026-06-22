namespace Shiro.Api.Models;

public sealed class ToolExecutionResult
{
    public required string ToolName { get; init; }

    public required bool Succeeded { get; init; }

    public required bool Simulated { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset ExecutedAtUtc { get; init; }
}
