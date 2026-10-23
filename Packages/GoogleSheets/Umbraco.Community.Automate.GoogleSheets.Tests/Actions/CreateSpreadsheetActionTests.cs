using System.Net;
using System.Text;
using System.Text.Json;
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

public class CreateSpreadsheetActionTests
{
    private const string CreateOk = """
        {
          "spreadsheetId": "NEW_SHEET_ID",
          "spreadsheetUrl": "https://docs.google.com/spreadsheets/d/NEW_SHEET_ID/edit"
        }
        """;

    [Fact]
    public async Task ExecuteAsync_posts_to_spreadsheets_endpoint_and_returns_id_and_url()
    {
        HttpRequestMessage? captured = null;
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, CreateOk, req => captured = req),
            new CreateSpreadsheetSettings { Title = "My Report" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri!.ToString().ShouldBe("https://sheets.googleapis.com/v4/spreadsheets");

        var output = (CreateSpreadsheetOutput)result.OutputData!;
        output.SpreadsheetId.ShouldBe("NEW_SHEET_ID");
        output.SpreadsheetUrl.ShouldBe("https://docs.google.com/spreadsheets/d/NEW_SHEET_ID/edit");
    }

    [Fact]
    public async Task ExecuteAsync_includes_title_in_request_body()
    {
        string? body = null;
        await BuildHarness(StubHandler(HttpStatusCode.OK, CreateOk, req =>
            body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult()),
            new CreateSpreadsheetSettings { Title = "Q4 Report" });

        body.ShouldNotBeNull();
        body.ShouldContain("\"Q4 Report\"");
    }

    [Fact]
    public async Task ExecuteAsync_includes_sheet_tabs_when_SheetNames_provided()
    {
        string? body = null;
        await BuildHarness(StubHandler(HttpStatusCode.OK, CreateOk, req =>
            body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult()),
            new CreateSpreadsheetSettings { Title = "Report", SheetNames = "Data, Summary, Config" });

        body.ShouldNotBeNull();
        using var doc = JsonDocument.Parse(body);
        var sheets = doc.RootElement.GetProperty("sheets");
        sheets.GetArrayLength().ShouldBe(3);
        sheets[0].GetProperty("properties").GetProperty("title").GetString().ShouldBe("Data");
        sheets[1].GetProperty("properties").GetProperty("title").GetString().ShouldBe("Summary");
        sheets[2].GetProperty("properties").GetProperty("title").GetString().ShouldBe("Config");
    }

    [Fact]
    public async Task ExecuteAsync_omits_sheets_array_when_SheetNames_blank()
    {
        string? body = null;
        await BuildHarness(StubHandler(HttpStatusCode.OK, CreateOk, req =>
            body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult()),
            new CreateSpreadsheetSettings { Title = "Report", SheetNames = "" });

        body.ShouldNotBeNull();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("sheets", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_falls_back_to_constructed_url_when_api_omits_spreadsheetUrl()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"spreadsheetId":"ABC123"}"""),
            new CreateSpreadsheetSettings { Title = "Report" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (CreateSpreadsheetOutput)result.OutputData!;
        output.SpreadsheetUrl.ShouldBe("https://docs.google.com/spreadsheets/d/ABC123/edit");
    }

    [Theory]
    [InlineData("Sheet1, Sheet2", new[] { "Sheet1", "Sheet2" })]
    [InlineData("  Data , Summary  ", new[] { "Data", "Summary" })]
    [InlineData("", new string[0])]
    [InlineData("  ", new string[0])]
    [InlineData("Single", new[] { "Single" })]
    public void ParseSheetNames_handles_various_inputs(string input, string[] expected) =>
        CreateSpreadsheetAction.ParseSheetNames(input).ShouldBe(expected);

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_title_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new CreateSpreadsheetSettings { Title = "" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<CreateSpreadsheetAction>()
            .WithService(CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}")))
            .WithService(creds.Object)
            .WithSettings(new CreateSpreadsheetSettings { Title = "Report" })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_invalid_response_when_api_returns_no_spreadsheet_id()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, """{"spreadsheetUrl":"https://example.com"}"""),
            new CreateSpreadsheetSettings { Title = "Report" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_permission_denied()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.Forbidden,
                """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}"""),
            new CreateSpreadsheetSettings { Title = "Report" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, CreateSpreadsheetSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<CreateSpreadsheetAction>()
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
