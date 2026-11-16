using System.Net;
using System.Web;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Pushover.Actions;
using Umbraco.Community.Automate.Pushover.Api;
using Umbraco.Community.Automate.Pushover.Connections;
using Umbraco.Community.Automate.Pushover.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Pushover.Tests.Actions;

public class SendNotificationActionTests
{
    private const string Accepted = """{ "status": 1, "request": "647d2300-702c-4b38-8b2f-d56326ae460b" }""";

    [Fact]
    public async Task Sends_the_notification_and_returns_the_request_id()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, Accepted));

        var result = await Run(handler, new SendNotificationSettings { Message = "New order", Title = "Shop" });

        Assert.Equal(ActionResultStatus.Success, result.Status);
        var output = Assert.IsType<SendNotificationOutput>(result.OutputData);
        Assert.Equal("1", output.Status);
        Assert.Equal("647d2300-702c-4b38-8b2f-d56326ae460b", output.Request);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.pushover.net/1/messages.json", request.RequestUri!.ToString());

        var form = HttpUtility.ParseQueryString(handler.Bodies.Single()!);
        Assert.Equal("app-token", form["token"]);
        Assert.Equal("user-key", form["user"]);
        Assert.Equal("New order", form["message"]);
        Assert.Equal("Shop", form["title"]);
        Assert.Equal("pushover", form["sound"]);
        Assert.Null(form["priority"]);
    }

    [Fact]
    public void Custom_sound_overrides_the_sound_dropdown()
    {
        var fields = SendNotificationAction.BuildFields(Connection(), new SendNotificationSettings { Message = "x", Sound = "bike", CustomSound = "kerching" });

        Assert.Equal("kerching", fields["sound"]);
    }

    [Fact]
    public void Url_and_url_title_are_sent_when_set()
    {
        var fields = SendNotificationAction.BuildFields(Connection(), new SendNotificationSettings { Message = "x", Url = "https://example.com", UrlTitle = "Open" });

        Assert.Equal("https://example.com", fields["url"]);
        Assert.Equal("Open", fields["url_title"]);
    }

    [Theory]
    [InlineData("Min", "-2")]
    [InlineData("Low", "-1")]
    [InlineData("Default", null)]
    [InlineData("High", "1")]
    [InlineData("Max", "2")]
    public void Priority_maps_to_pushovers_scale(string priority, string? expected)
    {
        var fields = SendNotificationAction.BuildFields(Connection(), new SendNotificationSettings { Message = "x", Priority = priority });

        Assert.Equal(expected, fields.GetValueOrDefault("priority"));
    }

    [Fact]
    public void Max_priority_sends_the_connections_retry_and_expire()
    {
        var connection = Connection();
        connection.Retry = 90;
        connection.Expire = 3600;

        var fields = SendNotificationAction.BuildFields(connection, new SendNotificationSettings { Message = "x", Priority = "Max" });

        Assert.Equal("90", fields["retry"]);
        Assert.Equal("3600", fields["expire"]);
    }

    [Fact]
    public void Lower_priorities_send_no_retry_or_expire()
    {
        var fields = SendNotificationAction.BuildFields(Connection(), new SendNotificationSettings { Message = "x", Priority = "High" });

        Assert.False(fields.ContainsKey("retry"));
        Assert.False(fields.ContainsKey("expire"));
    }

    [Fact]
    public async Task Blank_message_is_a_validation_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();

        var result = await Run(handler, new SendNotificationSettings { Message = " " });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Unresolved_reference_is_a_configuration_error_and_calls_nothing()
    {
        var handler = new StubHttpMessageHandler();
        var connection = Connection();
        connection.ApiToken = "$Umbraco:Automate:Secrets:Pushover:ApiToken";

        var result = await Run(handler, new SendNotificationSettings { Message = "x" }, connection);

        Assert.Equal(StepRunErrorCategory.ConfigurationError, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejected_request_reports_pushovers_reason()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.BadRequest,
            """{ "status": 0, "errors": ["application token is invalid"], "request": "r1" }"""));

        var result = await Run(handler, new SendNotificationSettings { Message = "x" });

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Contains("application token is invalid", result.Exception!.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.InternalServerError, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Temporary_failures_are_categorised_so_Automate_retries(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(status, """{ "status": 0 }"""));

        var result = await Run(handler, new SendNotificationSettings { Message = "x" });

        Assert.Equal(expected, result.ErrorCategory);
    }

    private static PushoverConnectionSettings Connection() => new() { ApiToken = "app-token", UserKey = "user-key" };

    private static Task<ActionResult> Run(StubHttpMessageHandler handler, SendNotificationSettings settings, PushoverConnectionSettings? connection = null)
        => ActionTestHarness.For<SendNotificationAction>()
            .WithService(new PushoverClient(new StubHttpClientFactory(handler)))
            .WithSettings(settings)
            .WithConnection("community.pushover", connection ?? Connection())
            .ExecuteAsync();
}
