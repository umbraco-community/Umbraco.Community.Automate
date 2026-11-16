using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Models;

/// <summary>httpbin.org's response to <c>GET /bearer</c>, used to test a connection.</summary>
public sealed class BearerResponse
{
    [JsonPropertyName("authenticated")]
    public bool Authenticated { get; set; }
}
