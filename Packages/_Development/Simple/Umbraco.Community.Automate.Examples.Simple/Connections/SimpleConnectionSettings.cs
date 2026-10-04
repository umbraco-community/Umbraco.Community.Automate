using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Examples.Simple.Configuration;

namespace Umbraco.Community.Automate.Examples.Simple.Connections;

/// <summary>The settings stored on a connection: just the API key.</summary>
public sealed class SimpleConnectionSettings
{
    // Defaults to a reference to configuration, so a new connection opens pre-filled and a key
    // stored in appsettings just works. Anyone can overwrite it with the key itself.
    [Field(
        Label = "API key",
        Description = "Any token works with httpbin.org; a real service would say where to get one.",
        IsSensitive = true,
        SortOrder = 0)]
    public string ApiKey { get; set; } = SimpleConfiguration.ApiKeyReference;
}
