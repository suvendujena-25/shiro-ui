namespace Shiro.Api.Services;

public interface IWeatherService
{
    Task<string> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken);
}
