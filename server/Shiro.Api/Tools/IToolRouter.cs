using Shiro.Api.Models;

namespace Shiro.Api.Tools;

public interface IToolRouter
{
    ToolRouteResult Route(string userMessage);
}
