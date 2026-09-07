using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Skoda.Connections;

public sealed class SkodaConnectionSettings
{
    [Field(
        Label = "API key",
        Description = "The API key generated in the MyŠkoda app.",
        IsSensitive = true,
        SortOrder = 0)]
    public string ApiKey { get; set; } = string.Empty;

    [Field(
        Label = "VIN",
        Description = "The Vehicle Identification Number of the Škoda vehicle.",
        SortOrder = 1)]
    public string Vin { get; set; } = string.Empty;

    [Field(
        Label = "Validate connection",
        Description = "Test the API key and VIN against the Škoda API when validating this connection. Each test uses one API request, so you can disable this to avoid consuming the limited request quota.",
        SortOrder = 2)]
    public bool ValidateConnection { get; set; }
}