using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.GoogleSheets.Actions;
using Umbraco.Community.Automate.GoogleSheets.Connection;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests.Actions;

public class ClearRangeActionTests
{
    [Fact]
    public async Task ExecuteAsync_posts_to_clear_endpoint_and_returns_cleared_range()
    {
        HttpRequestMessage? captured = null;
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK,
                """{"spreadsheetId":"SHEET_ID","clearedRange":"Sheet1!A1:Z1000"}""",
                req => captured = req),
            new ClearRangeSettings
            {
                SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
                SheetName = "Sheet1",
            });

        result.Status.ShouldBe(ActionResultStatus.Success);
        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1:clear");

        var output = (ClearRangeOutput)result.OutputData!;
        output.ClearedRange.ShouldBe("Sheet1!A1:Z1000");
    }

    [Fact]
    public async Task ExecuteAsync_appends_range_to_url_when_range_specified()
    {
        HttpRequestMessage? captured = null;
        await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"clearedRange":"Sheet1!A2:Z1000"}""", req => captured = req),
            new ClearRangeSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Range = "A2:Z1000" });

        captured!.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1%21A2%3AZ1000:clear");
    }

    [Fact]
    public async Task ExecuteAsync_succeeds_with_null_cleared_range_when_response_body_is_empty()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{}"""),
            new ClearRangeSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        ((ClearRangeOutput)result.OutputData!).ClearedRange.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new ClearRangeSettings { SpreadsheetId = "", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_sheet_name_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new ClearRangeSettings { SpreadsheetId = "SHEET_ID", SheetName = "" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_url_is_unrelated_site()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new ClearRangeSettings { SpreadsheetId = "https://www.dropbox.com/abc", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<ClearRangeAction>()
            .WithService(CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}")))
            .WithService(creds.Object)
            .WithSettings(new ClearRangeSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_permission_denied()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.Forbidden,
                """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}"""),
            new ClearRangeSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, ClearRangeSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<ClearRangeAction>()
            .WithService(CreateHttpClientFactory(handler))
            .WithService(creds.Object)
            .WithSettings(settings)
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UmbracoAutomate")).Returns(client);
        return factory.Object;
    }

    private static HttpMessageHandler StubHandler(HttpStatusCode code, string json, Action<HttpRequestMessage>? capture = null)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture?.Invoke(req);
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }
}
