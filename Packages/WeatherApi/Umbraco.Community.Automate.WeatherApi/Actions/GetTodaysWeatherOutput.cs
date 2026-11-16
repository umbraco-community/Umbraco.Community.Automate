namespace Umbraco.Community.Automate.WeatherApi.Actions;

public sealed class GetTodaysWeatherOutput
{
    public string? LocationName { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; }
    public string? LocalTime { get; init; }
    public string? Date { get; init; }
    public double MaxTemperatureC { get; init; }
    public double MaxTemperatureF { get; init; }
    public double MinTemperatureC { get; init; }
    public double MinTemperatureF { get; init; }
    public double AvgTemperatureC { get; init; }
    public double AvgTemperatureF { get; init; }
    public double MaxWindMph { get; init; }
    public double MaxWindKph { get; init; }
    public double TotalPrecipMm { get; init; }
    public double TotalPrecipIn { get; init; }
    public double TotalSnowCm { get; init; }
    public double AvgVisKm { get; init; }
    public double AvgVisMiles { get; init; }
    public int AvgHumidity { get; init; }
    public bool WillItRain { get; init; }
    public int ChanceOfRain { get; init; }
    public bool WillItSnow { get; init; }
    public int ChanceOfSnow { get; init; }
    public string? Condition { get; init; }
    public string? ConditionIconUrl { get; init; }
    public int ConditionCode { get; init; }
    public double Uv { get; init; }

    /// <summary>The full, unprocessed JSON response returned by WeatherAPI.com.</summary>
    public string? RawResponse { get; init; }
}
