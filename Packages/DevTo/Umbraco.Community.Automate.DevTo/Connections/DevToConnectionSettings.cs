using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.DevTo.Connections;

public sealed class DevToConnectionSettings
{
    public const string DefaultInstanceUrl = "https://dev.to";

    [Field(Label = "API Key",
        Description = "Your DEV API key, from [Settings → Extensions](https://dev.to/settings/extensions) on DEV. " +
                      "Recommended: reference a secret instead of pasting it, e.g. $Umbraco:Automate:Secrets:DevTo:ApiKey",
        SortOrder = 0,
        IsSensitive = true)]
    public string ApiKey { get; set; } = string.Empty;

    [Field(Label = "Instance URL",
        Description = "Base URL of the Forem instance. Leave as https://dev.to unless you post to another Forem community.",
        SortOrder = 1,
        Group = "Advanced")]
    public string InstanceUrl { get; set; } = DefaultInstanceUrl;
}
