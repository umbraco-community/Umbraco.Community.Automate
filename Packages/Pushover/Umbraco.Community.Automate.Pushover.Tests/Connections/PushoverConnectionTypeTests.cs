using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Pushover.Api;
using Umbraco.Community.Automate.Pushover.Connections;
using Umbraco.Community.Automate.Pushover.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Pushover.Tests.Connections;

public class PushoverConnectionTypeTests
{
    [Fact]
    public void New_connections_start_with_the_configuration_references()
    {
        var settings = new PushoverConnectionSettings();

        Assert.Equal("$Umbraco:Automate:Secrets:Pushover:ApiToken", settings.ApiToken);
        Assert.Equal("$Umbraco:Automate:Secrets:Pushover:UserKey", settings.UserKey);
        Assert.Equal(60, settings.Retry);
        Assert.Equal(1800, settings.Expire);
    }

    [Fact]
    public async Task Accepted_token_and_key_connect_and_list_the_devices()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK,
            """{ "status": 1, "devices": ["iphone", "desktop"], "request": "r1" }"""));

        var result = await Validate(handler, new PushoverConnectionSettings { ApiToken = "app-token", UserKey = "user-key" });

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        Assert.Contains("iphone, desktop", result.Message);
        Assert.Equal("https://api.pushover.net/1/users/validate.json", handler.Requests.Single().RequestUri!.ToString());
    }

    [Theory]
    [InlineData("", "user-key", "API token")]
    [InlineData("app-token", "", "user or group key")]
    [InlineData("$Umbraco:Automate:Secrets:Pushover:ApiToken", "user-key", "could not be resolved")]
    public async Task Missing_or_unresolved_values_fail_without_calling_pushover(string token, string key, string expected)
    {
        var handler = new StubHttpMessageHandler();

        var result = await Validate(handler, new PushoverConnectionSettings { ApiToken = token, UserKey = key });

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Contains(expected, result.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejected_key_fails_with_pushovers_reason()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.BadRequest,
            """{ "status": 0, "user": "invalid", "errors": ["user key is invalid"], "request": "r1" }"""));

        var result = await Validate(handler, new PushoverConnectionSettings { ApiToken = "app-token", UserKey = "nope" });

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Contains("user key is invalid", result.Message);
    }

    private static Task<ConnectionValidationResult> Validate(HttpMessageHandler handler, PushoverConnectionSettings settings)
        => new PushoverConnectionType(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new PushoverClient(new StubHttpClientFactory(handler)))
            .ValidateAsync(settings, CancellationToken.None);
}
