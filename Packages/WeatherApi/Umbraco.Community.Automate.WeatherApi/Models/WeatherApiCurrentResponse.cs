using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.WeatherApi.Models;

internal class WeatherApiCurrentResponse
{
    [JsonPropertyName("location")]
    public WeatherApiLocation? Location { get; set; }

    [JsonPropertyName("current")]
    public WeatherApiCurrent? Current { get; set; }
}

internal class WeatherApiLocation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("localtime")]
    public string? Localtime { get; set; }
}

internal class WeatherApiCurrent
{
    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("temp_f")]
    public double TempF { get; set; }

    [JsonPropertyName("condition")]
    public WeatherApiCondition? Condition { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("cloud")]
    public int Cloud { get; set; }

    [JsonPropertyName("wind_kph")]
    public double WindKph { get; set; }

    [JsonPropertyName("wind_mph")]
    public double WindMph { get; set; }

    [JsonPropertyName("wind_dir")]
    public string? WindDir { get; set; }

    [JsonPropertyName("will_it_rain")]
    public int WillItRain { get; set; }

    [JsonPropertyName("chance_of_rain")]
    public int ChanceOfRain { get; set; }

    [JsonPropertyName("will_it_snow")]
    public int WillItSnow { get; set; }

    [JsonPropertyName("chance_of_snow")]
    public int ChanceOfSnow { get; set; }

    [JsonPropertyName("uv")]
    public double Uv { get; set; }
}

/// <summary>E.g. <c>{ "text": "Sunny", "icon": "//cdn.weatherapi.com/weather/64x64/day/113.png", "code": 1000 }</c>.</summary>
internal class WeatherApiCondition
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

internal class WeatherApiErrorResponse
{
    [JsonPropertyName("error")]
    public WeatherApiError? Error { get; set; }
}

internal class WeatherApiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
