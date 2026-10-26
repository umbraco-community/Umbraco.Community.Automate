using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

public class GetTodaysWeatherSettings
{
    [Field(
        Label = "Location",
        Description = "The location to get today's weather forecast for. Accepts a city name, US zip, UK postcode, IP address, or \"lat,lon\". Supports bindings.",
        SupportsBindings = true,
        SortOrder = 1)]
    public string Location { get; set; } = string.Empty;

    [Field(
        Label = "Culture",
        Description = "Optional. The culture to localize the weather condition text into, e.g. \"en-GB\", \"fr-FR\", \"es-ES\". Falls back to English if not set. Supports bindings.",
        SupportsBindings = true,
        SortOrder = 2)]
    public string? Culture { get; set; }
}
