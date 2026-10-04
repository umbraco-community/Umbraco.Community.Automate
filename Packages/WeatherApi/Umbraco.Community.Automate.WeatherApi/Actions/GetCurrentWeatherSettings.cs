using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.WeatherApi.Actions;

/// <summary>
/// Defines the configurable settings for the Get Current Weather action in Umbraco Automate.
/// </summary>
public class GetCurrentWeatherSettings
{
    /// <summary>The location to get current weather for. Required.</summary>
    [Field(
        Label = "Location",
        Description = "The location to get current weather for. Accepts a city name, US zip, UK postcode, IP address, or \"lat,lon\". Supports bindings.",
        SupportsBindings = true,
        SortOrder = 1)]
    public string Location { get; set; } = string.Empty;

    /// <summary>The culture to localize the weather condition text into. Optional.</summary>
    [Field(
        Label = "Culture",
        Description = "Optional. The culture to localize the weather condition text into, e.g. \"en-GB\", \"fr-FR\", \"es-ES\". Falls back to English if not set. Supports bindings.",
        SupportsBindings = true,
        SortOrder = 2)]
    public string? Culture { get; set; }
}
