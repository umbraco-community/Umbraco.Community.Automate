using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.WeatherApi.Models;

/// <summary>
/// Represents a successful response from the WeatherAPI.com forecast endpoint, requested with
/// <c>days=1</c> to retrieve today's forecast.
/// </summary>
internal class WeatherApiForecastResponse
{
    [JsonPropertyName("location")]
    public WeatherApiLocation? Location { get; set; }

    [JsonPropertyName("forecast")]
    public WeatherApiForecast? Forecast { get; set; }
}

/// <summary>
/// The list of forecast days returned by the forecast endpoint.
/// </summary>
internal class WeatherApiForecast
{
    [JsonPropertyName("forecastday")]
    public List<WeatherApiForecastDay>? ForecastDay { get; set; }
}

/// <summary>
/// A single day's forecast, including the date and aggregated day conditions.
/// </summary>
internal class WeatherApiForecastDay
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("day")]
    public WeatherApiDay? Day { get; set; }
}

/// <summary>
/// The aggregated day weather conditions for a forecast day.
/// </summary>
internal class WeatherApiDay
{
    [JsonPropertyName("maxtemp_c")]
    public double MaxTempC { get; set; }

    [JsonPropertyName("maxtemp_f")]
    public double MaxTempF { get; set; }

    [JsonPropertyName("mintemp_c")]
    public double MinTempC { get; set; }

    [JsonPropertyName("mintemp_f")]
    public double MinTempF { get; set; }

    [JsonPropertyName("avgtemp_c")]
    public double AvgTempC { get; set; }

    [JsonPropertyName("avgtemp_f")]
    public double AvgTempF { get; set; }

    [JsonPropertyName("maxwind_mph")]
    public double MaxWindMph { get; set; }

    [JsonPropertyName("maxwind_kph")]
    public double MaxWindKph { get; set; }

    [JsonPropertyName("totalprecip_mm")]
    public double TotalPrecipMm { get; set; }

    [JsonPropertyName("totalprecip_in")]
    public double TotalPrecipIn { get; set; }

    [JsonPropertyName("totalsnow_cm")]
    public double TotalSnowCm { get; set; }

    [JsonPropertyName("avgvis_km")]
    public double AvgVisKm { get; set; }

    [JsonPropertyName("avgvis_miles")]
    public double AvgVisMiles { get; set; }

    [JsonPropertyName("avghumidity")]
    public int AvgHumidity { get; set; }

    [JsonPropertyName("daily_will_it_rain")]
    public int DailyWillItRain { get; set; }

    [JsonPropertyName("daily_chance_of_rain")]
    public int DailyChanceOfRain { get; set; }

    [JsonPropertyName("daily_will_it_snow")]
    public int DailyWillItSnow { get; set; }

    [JsonPropertyName("daily_chance_of_snow")]
    public int DailyChanceOfSnow { get; set; }

    [JsonPropertyName("condition")]
    public WeatherApiCondition? Condition { get; set; }

    [JsonPropertyName("uv")]
    public double Uv { get; set; }
}
