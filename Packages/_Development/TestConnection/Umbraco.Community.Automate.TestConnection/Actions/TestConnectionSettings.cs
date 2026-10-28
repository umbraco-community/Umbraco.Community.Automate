using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.TestConnection.Actions;

public sealed class TestConnectionSettings
{
    [Field(Label = "Connection", Description = "The alias of the connection to test, e.g. pushover. Find it under Automation → Settings → Connections.", SortOrder = 10)]
    public string ConnectionAlias { get; set; } = string.Empty;
}
