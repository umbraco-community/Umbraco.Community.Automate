namespace Umbraco.Community.Automate.WeatherApi.Tests.Fakes;

/// <summary>Trimmed WeatherAPI.com response bodies, shaped like the real API's.</summary>
public static class WeatherApiResponses
{
    public const string Current = """
        {
          "location": { "name": "London", "region": "City of London, Greater London", "country": "United Kingdom", "localtime": "2026-10-01 12:00" },
          "current": {
            "last_updated": "2026-10-01 11:45", "temp_c": 14.0, "temp_f": 57.2, "humidity": 72, "cloud": 50,
            "wind_kph": 15.1, "wind_mph": 9.4, "wind_dir": "WSW", "uv": 3.0,
            "condition": { "text": "Partly cloudy", "icon": "//cdn.weatherapi.com/weather/64x64/day/116.png", "code": 1003 }
          }
        }
        """;

    public const string Forecast = """
        {
          "location": { "name": "London", "region": "City of London, Greater London", "country": "United Kingdom", "localtime": "2026-10-01 12:00" },
          "forecast": {
            "forecastday": [
              {
                "date": "2026-10-01",
                "day": {
                  "maxtemp_c": 17.2, "maxtemp_f": 63.0, "mintemp_c": 9.1, "mintemp_f": 48.4, "avgtemp_c": 13.0, "avgtemp_f": 55.4,
                  "maxwind_mph": 12.1, "maxwind_kph": 19.4, "totalprecip_mm": 1.2, "totalprecip_in": 0.05, "totalsnow_cm": 0,
                  "avgvis_km": 10, "avgvis_miles": 6, "avghumidity": 70,
                  "daily_will_it_rain": 1, "daily_chance_of_rain": 80, "daily_will_it_snow": 0, "daily_chance_of_snow": 0,
                  "uv": 2.0,
                  "condition": { "text": "Patchy rain nearby", "icon": "//cdn.weatherapi.com/weather/64x64/day/176.png", "code": 1063 }
                }
              }
            ]
          }
        }
        """;

    public const string InvalidKey = """{ "error": { "code": 2006, "message": "API key is invalid." } }""";

    public const string NoLocation = """{ "error": { "code": 1006, "message": "No matching location found." } }""";
}
