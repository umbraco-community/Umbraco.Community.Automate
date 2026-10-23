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

public class CreateSheetActionTests
{
    private const string CreateOk = """
        {
          "spreadsheetId": "SHEET_ID",
          "replies": [
            {
              "addSheet": {
                "properties": {
                  "sheetId": 987654,
                  "title": "January 2025"
                }
              }
            }
          ]
        }
        """;

    [Fact]
    public async Task ExecuteAsync_posts_batch_update_and_returns_sheet_id_and_title()
    {
        HttpRequestMessage? captured = null;
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, CreateOk, req => captured = req),
            new CreateSheetSettings
            {
                SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
                SheetTitle = "January 2025",
            });

        result.Status.ShouldBe(ActionResultStatus.Success);
        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID:batchUpdate");

        var output = (CreateSheetOutput)result.OutputData!;
        output.SheetId.ShouldBe(987654);
        output.SheetTitle.ShouldBe("January 2025");
    }

    [Fact]
    public async Task ExecuteAsync_includes_addSheet_request_with_title_in_payload()
    {
        string? body = null;
        await BuildHarness(StubHandler(HttpStatusCode.OK, CreateOk, req =>
            body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult()),
            new CreateSheetSettings { SpreadsheetId = "SHEET_ID", SheetTitle = "My Tab" });

        body.ShouldNotBeNull();
        body.ShouldContain("\"addSheet\"");
        body.ShouldContain("\"My Tab\"");
    }

    [Fact]
    public async Task ExecuteAsync_falls_back_to_settings_title_when_reply_omits_properties()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"replies":[{}]}"""),
            new CreateSheetSettings { SpreadsheetId = "SHEET_ID", SheetTitle = "Fallback Tab" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (CreateSheetOutput)result.OutputData!;
        output.SheetId.ShouldBe(0);
        output.SheetTitle.ShouldBe("Fallback Tab");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new CreateSheetSettings { SpreadsheetId = "", SheetTitle = "Tab" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_sheet_title_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new CreateSheetSettings { SpreadsheetId = "SHEET_ID", SheetTitle = "" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_url_is_unrelated_site()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new CreateSheetSettings { SpreadsheetId = "https://www.dropbox.com/abc", SheetTitle = "Tab" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("doesn't look like a Google Sheets link");
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<CreateSheetAction>()
            .WithService(CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}")))
            .WithService(creds.Object)
            .WithSettings(new CreateSheetSettings { SpreadsheetId = "SHEET_ID", SheetTitle = "Tab" })
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
            new CreateSheetSettings { SpreadsheetId = "SHEET_ID", SheetTitle = "Tab" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, CreateSheetSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<CreateSheetAction>()
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
