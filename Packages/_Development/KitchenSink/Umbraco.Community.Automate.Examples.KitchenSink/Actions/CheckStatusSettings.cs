using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>Settings for <see cref="CheckStatusAction"/>.</summary>
public sealed class CheckStatusSettings
{
    [Field(
        Label = "Status code",
        Description = "The HTTP status httpbin.org should answer with, e.g. 200 or 404, to try each outcome.",
        SortOrder = 0)]
    public int StatusCode { get; set; } = 200;
}
