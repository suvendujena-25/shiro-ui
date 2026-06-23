using System.Globalization;
using System.Text.Json;

namespace Shiro.Api.Services;

public sealed class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient httpClient;

    public OpenMeteoWeatherService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<string> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken)
    {
        var place = await FindLocationAsync(location, cancellationToken);

        if (place is null)
        {
            return $"I could not find weather coordinates for '{location}'. Try a city name like Mumbai, Pune, London, or New York.";
        }

        var forecastUri =
            "https://api.open-meteo.com/v1/forecast"
            + $"?latitude={place.Latitude.ToString(CultureInfo.InvariantCulture)}"
            + $"&longitude={place.Longitude.ToString(CultureInfo.InvariantCulture)}"
            + "&current=temperature_2m,precipitation,rain,weather_code"
            + "&daily=precipitation_probability_max"
            + "&timezone=auto";

        using var response = await httpClient.GetAsync(forecastUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return "I reached the weather service, but it could not return the forecast right now.";
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!root.TryGetProperty("current", out var current))
        {
            return "I reached the weather service, but the forecast response was missing current weather details.";
        }

        var temperature = ReadDouble(current, "temperature_2m");
        var rain = ReadDouble(current, "rain");
        var precipitation = ReadDouble(current, "precipitation");
        var code = ReadInt(current, "weather_code");
        var rainChance = ReadDailyRainChance(root);
        var condition = DescribeWeather(code);
        var rainSummary = rain > 0 || precipitation > 0
            ? "It is reporting rain or precipitation right now."
            : rainChance is >= 50
                ? $"It may rain today. Max precipitation chance is around {rainChance}%."
                : rainChance is not null
                    ? $"Rain chance today looks low to moderate at around {rainChance}%."
                    : "No current rain is reported right now.";

        return $"Weather for {place.Name}: {condition}, {temperature:0.#}°C. {rainSummary}";
    }

    private async Task<WeatherLocation?> FindLocationAsync(string location, CancellationToken cancellationToken)
    {
        var uri =
            "https://geocoding-api.open-meteo.com/v1/search"
            + $"?name={Uri.EscapeDataString(location)}"
            + "&count=1&language=en&format=json";

        using var response = await httpClient.GetAsync(uri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array
            || results.GetArrayLength() == 0)
        {
            return null;
        }

        var first = results[0];
        var name = first.GetProperty("name").GetString() ?? location;
        var country = first.TryGetProperty("country", out var countryElement)
            ? countryElement.GetString()
            : null;

        return new WeatherLocation(
            string.IsNullOrWhiteSpace(country) ? name : $"{name}, {country}",
            first.GetProperty("latitude").GetDouble(),
            first.GetProperty("longitude").GetDouble());
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            ? property.GetDouble()
            : 0;
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : 0;
    }

    private static int? ReadDailyRainChance(JsonElement root)
    {
        if (!root.TryGetProperty("daily", out var daily)
            || !daily.TryGetProperty("precipitation_probability_max", out var probabilities)
            || probabilities.ValueKind != JsonValueKind.Array
            || probabilities.GetArrayLength() == 0
            || probabilities[0].ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return probabilities[0].GetInt32();
    }

    private static string DescribeWeather(int code)
    {
        return code switch
        {
            0 => "clear sky",
            1 or 2 or 3 => "partly cloudy",
            45 or 48 => "foggy",
            51 or 53 or 55 or 56 or 57 => "drizzle",
            61 or 63 or 65 or 66 or 67 => "rain",
            71 or 73 or 75 or 77 => "snow",
            80 or 81 or 82 => "rain showers",
            95 or 96 or 99 => "thunderstorm",
            _ => "current conditions available"
        };
    }

    private sealed record WeatherLocation(string Name, double Latitude, double Longitude);
}
