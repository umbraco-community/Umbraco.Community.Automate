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

public class GetRowsActionTests
{
    private const string SheetData = """
        {
          "values": [
            ["Name", "Email", "Status"],
            ["Alice", "alice@example.com", "Active"],
            ["Bob", "bob@example.com", "Inactive"]
          ]
        }
        """;

    [Fact]
    public async Task ExecuteAsync_returns_all_rows_and_correct_row_count()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, SheetData),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", HasHeaderRow = false });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetRowsOutput)result.OutputData!;
        output.RowCount.ShouldBe(3);
        output.Rows.Count.ShouldBe(3);
        output.Rows[0].ShouldBe(["Name", "Email", "Status"]);
        output.Headers.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_default_HasHeaderRow_separates_the_header_row()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, SheetData),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetRowsOutput)result.OutputData!;
        output.Headers.ShouldBe(["Name", "Email", "Status"]);
        output.RowCount.ShouldBe(2);
        output.Rows.Count.ShouldBe(2);
        output.Rows[0].ShouldBe(["Alice", "alice@example.com", "Active"]);
    }

    [Fact]
    public async Task ExecuteAsync_separates_header_row_when_HasHeaderRow_is_true()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, SheetData),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", HasHeaderRow = true });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetRowsOutput)result.OutputData!;
        output.Headers.ShouldBe(["Name", "Email", "Status"]);
        output.RowCount.ShouldBe(2);
        output.Rows.Count.ShouldBe(2);
        output.Rows[0].ShouldBe(["Alice", "alice@example.com", "Active"]);
    }

    [Fact]
    public async Task ExecuteAsync_builds_correct_url_when_no_range_specified()
    {
        HttpRequestMessage? captured = null;
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, SheetData, req => captured = req),
            new GetRowsSettings
            {
                SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
                SheetName = "Sheet1",
            });

        result.Status.ShouldBe(ActionResultStatus.Success);
        captured!.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1");
    }

    [Fact]
    public async Task ExecuteAsync_appends_range_to_url_when_range_specified()
    {
        HttpRequestMessage? captured = null;
        await BuildHarness(StubHandler(HttpStatusCode.OK, SheetData, req => captured = req),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Range = "A2:D50" });

        captured!.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1%21A2%3AD50");
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_output_when_sheet_has_no_data()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, """{}"""),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetRowsOutput)result.OutputData!;
        output.RowCount.ShouldBe(0);
        output.Rows.ShouldBeEmpty();
        output.Headers.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_rows_and_header_only_when_sheet_has_only_header()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"values":[["Name","Email"]]}"""),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", HasHeaderRow = true });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetRowsOutput)result.OutputData!;
        output.Headers.ShouldBe(["Name", "Email"]);
        output.RowCount.ShouldBe(0);
        output.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new GetRowsSettings { SpreadsheetId = "", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_sheet_name_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_url_is_unrelated_site()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new GetRowsSettings { SpreadsheetId = "https://www.dropbox.com/s/abc/file.xlsx", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<GetRowsAction>()
            .WithService(CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}")))
            .WithService(creds.Object)
            .WithSettings(new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" })
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
            new GetRowsSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, GetRowsSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<GetRowsAction>()
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
