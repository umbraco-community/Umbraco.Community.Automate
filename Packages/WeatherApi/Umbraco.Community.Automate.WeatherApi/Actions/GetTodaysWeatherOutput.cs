namespace Umbraco.Community.Automate.WeatherApi.Actions;

/// <summary>
/// Output produced by the Get Today's Weather action.
/// </summary>
public sealed class GetTodaysWeatherOutput
{
    /// <summary>Gets the resolved location name, e.g. "London".</summary>
    public string? LocationName { get; init; }

    /// <summary>Gets the resolved region, e.g. "City of London, Greater London".</summary>
    public string? Region { get; init; }

    /// <summary>Gets the resolved country, e.g. "United Kingdom".</summary>
    public string? Country { get; init; }

    /// <summary>Gets the local time at the resolved location.</summary>
    public string? LocalTime { get; init; }

    /// <summary>Gets the forecast date, e.g. "2024-01-01".</summary>
    public string? Date { get; init; }

    /// <summary>Gets today's maximum temperature in degrees Celsius.</summary>
    public double MaxTemperatureC { get; init; }

    /// <summary>Gets today's maximum temperature in degrees Fahrenheit.</summary>
    public double MaxTemperatureF { get; init; }

    /// <summary>Gets today's minimum temperature in degrees Celsius.</summary>
    public double MinTemperatureC { get; init; }

    /// <summary>Gets today's minimum temperature in degrees Fahrenheit.</summary>
    public double MinTemperatureF { get; init; }

    /// <summary>Gets today's average temperature in degrees Celsius.</summary>
    public double AvgTemperatureC { get; init; }

    /// <summary>Gets today's average temperature in degrees Fahrenheit.</summary>
    public double AvgTemperatureF { get; init; }

    /// <summary>Gets today's maximum wind speed in miles per hour.</summary>
    public double MaxWindMph { get; init; }

    /// <summary>Gets today's maximum wind speed in kilometres per hour.</summary>
    public double MaxWindKph { get; init; }

    /// <summary>Gets today's total precipitation in millimetres.</summary>
    public double TotalPrecipMm { get; init; }

    /// <summary>Gets today's total precipitation in inches.</summary>
    public double TotalPrecipIn { get; init; }

    /// <summary>Gets today's total snowfall in centimetres.</summary>
    public double TotalSnowCm { get; init; }

    /// <summary>Gets today's average visibility in kilometres.</summary>
    public double AvgVisKm { get; init; }

    /// <summary>Gets today's average visibility in miles.</summary>
    public double AvgVisMiles { get; init; }

    /// <summary>Gets today's average humidity as a percentage.</summary>
    public int AvgHumidity { get; init; }

    /// <summary>Gets whether it is expected to rain at some point today.</summary>
    public bool WillItRain { get; init; }

    /// <summary>Gets the chance of rain at some point today, as a percentage.</summary>
    public int ChanceOfRain { get; init; }

    /// <summary>Gets whether it is expected to snow at some point today.</summary>
    public bool WillItSnow { get; init; }

    /// <summary>Gets the chance of snow at some point today, as a percentage.</summary>
    public int ChanceOfSnow { get; init; }

    /// <summary>Gets the weather condition text, e.g. "Partly Cloudy".</summary>
    public string? Condition { get; init; }

    /// <summary>Gets the URL of the icon representing the weather condition.</summary>
    public string? ConditionIconUrl { get; init; }

    /// <summary>Gets the WeatherAPI.com condition code, e.g. 1003 for "Partly Cloudy".</summary>
    public int ConditionCode { get; init; }

    /// <summary>Gets the UV index.</summary>
    public double Uv { get; init; }

    /// <summary>Gets the full, unprocessed JSON response returned by WeatherAPI.com.</summary>
    public string? RawResponse { get; init; }
}
