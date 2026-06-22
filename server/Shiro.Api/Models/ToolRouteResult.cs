namespace Shiro.Api.Models;

public sealed class ToolRouteResult
{
    public bool HasToolRequest => ToolRequest is not null;

    public ToolRequest? ToolRequest { get; init; }

    public static ToolRouteResult NoTool() => new();

    public static ToolRouteResult FromToolRequest(ToolRequest toolRequest)
    {
        return new ToolRouteResult
        {
            ToolRequest = toolRequest
        };
    }
}
