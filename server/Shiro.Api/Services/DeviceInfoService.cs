using System.Globalization;

namespace Shiro.Api.Services;

public sealed class DeviceInfoService : IDeviceInfoService
{
    public string GetCurrentDateTimeMessage()
    {
        var now = DateTimeOffset.Now;
        var timeZone = TimeZoneInfo.Local;
        var dateText = now.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        var timeText = now.ToString("h:mm tt", CultureInfo.InvariantCulture);

        return $"Today is {dateText}. The current local time is {timeText} ({timeZone.DisplayName}).";
    }
}
