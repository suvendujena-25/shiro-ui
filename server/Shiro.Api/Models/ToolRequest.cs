namespace Shiro.Api.Models;

public sealed class ToolRequest
{
    public required string ToolName { get; init; }

    public required bool RequiresApproval { get; init; }

    public required string Reason { get; init; }

    public Dictionary<string, string> Arguments { get; init; } = [];
}
