using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.DevTo.Configuration;

namespace Umbraco.Community.Automate.DevTo.Connections;

public sealed class DevToConnectionSettings
{
    [Field(Label = "API Key",
        Description = "Your DEV API key, from [Settings → Extensions](https://dev.to/settings/extensions) on DEV.",
        SortOrder = 0,
        IsSensitive = true)]
    public string ApiKey { get; set; } = DevToConfiguration.ApiKeyReference;

    [Field(Label = "Instance URL",
        Description = "Base URL of the Forem instance. Leave as https://dev.to unless you post to another Forem community.",
        SortOrder = 1,
        Group = "Advanced")]
    public string InstanceUrl { get; set; } = DevToConfiguration.DefaultInstanceUrl;
}
