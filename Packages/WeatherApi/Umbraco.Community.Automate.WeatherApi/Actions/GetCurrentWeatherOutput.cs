namespace Umbraco.Community.Automate.WeatherApi.Actions;

/// <summary>
/// Output produced by the Get Current Weather action.
/// </summary>
public sealed class GetCurrentWeatherOutput
{
    /// <summary>Gets the resolved location name, e.g. "London".</summary>
    public string? LocationName { get; init; }

    /// <summary>Gets the resolved region, e.g. "City of London, Greater London".</summary>
    public string? Region { get; init; }

    /// <summary>Gets the resolved country, e.g. "United Kingdom".</summary>
    public string? Country { get; init; }

    /// <summary>Gets the local time at the resolved location.</summary>
    public string? LocalTime { get; init; }

    /// <summary>Gets the current temperature in degrees Celsius.</summary>
    public double TemperatureC { get; init; }

    /// <summary>Gets the current temperature in degrees Fahrenheit.</summary>
    public double TemperatureF { get; init; }

    /// <summary>Gets the weather condition text, e.g. "Sunny".</summary>
    public string? Condition { get; init; }

    /// <summary>Gets the URL of the icon representing the weather condition.</summary>
    public string? ConditionIconUrl { get; init; }

    /// <summary>Gets the WeatherAPI.com condition code, e.g. 1000 for "Sunny".</summary>
    public int ConditionCode { get; init; }

    /// <summary>Gets the current humidity as a percentage.</summary>
    public int Humidity { get; init; }

    /// <summary>Gets the current cloud cover as a percentage.</summary>
    public int Cloud { get; init; }

    /// <summary>Gets the current wind speed in kilometres per hour.</summary>
    public double WindKph { get; init; }

    /// <summary>Gets the current wind speed in miles per hour.</summary>
    public double WindMph { get; init; }

    /// <summary>Gets the current wind direction, e.g. "WSW".</summary>
    public string? WindDirection { get; init; }

    /// <summary>Gets the time the weather data was last updated.</summary>
    public string? LastUpdated { get; init; }

    /// <summary>Gets whether it is expected to rain during the current hour.</summary>
    public bool WillItRain { get; init; }

    /// <summary>Gets the chance of rain during the current hour, as a percentage.</summary>
    public int ChanceOfRain { get; init; }

    /// <summary>Gets whether it is expected to snow during the current hour.</summary>
    public bool WillItSnow { get; init; }

    /// <summary>Gets the chance of snow during the current hour, as a percentage.</summary>
    public int ChanceOfSnow { get; init; }

    /// <summary>Gets the UV index.</summary>
    public double Uv { get; init; }

    /// <summary>Gets the full, unprocessed JSON response returned by WeatherAPI.com.</summary>
    public string? RawResponse { get; init; }
}
