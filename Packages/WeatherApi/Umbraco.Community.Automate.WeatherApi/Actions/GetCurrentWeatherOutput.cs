namespace Umbraco.Community.Automate.WeatherApi.Actions;

public sealed class GetCurrentWeatherOutput
{
    public string? LocationName { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; }
    public string? LocalTime { get; init; }
    public double TemperatureC { get; init; }
    public double TemperatureF { get; init; }
    public string? Condition { get; init; }
    public string? ConditionIconUrl { get; init; }
    public int ConditionCode { get; init; }
    public int Humidity { get; init; }
    public int Cloud { get; init; }
    public double WindKph { get; init; }
    public double WindMph { get; init; }
    public string? WindDirection { get; init; }
    public string? LastUpdated { get; init; }
    public bool WillItRain { get; init; }
    public int ChanceOfRain { get; init; }
    public bool WillItSnow { get; init; }
    public int ChanceOfSnow { get; init; }
    public double Uv { get; init; }

    /// <summary>The full, unprocessed JSON response returned by WeatherAPI.com.</summary>
    public string? RawResponse { get; init; }
}
