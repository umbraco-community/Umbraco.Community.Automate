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

public class FindRowActionTests
{
    [Fact]
    public async Task ExecuteAsync_returns_found_outcome_with_row_data_when_value_matches()
    {
        var handler = StubHandler(HttpStatusCode.OK, """
            {
              "range": "Sheet1!A1:Z3",
              "majorDimension": "ROWS",
              "values": [
                ["Name", "Email"],
                ["Alice", "alice@example.com"],
                ["Bob", "bob@example.com"]
              ]
            }
            """);

        var result = await BuildHarness(handler,
            new FindRowSettings
            {
                SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
                SheetName = "Sheet1",
                SearchColumn = "A",
                SearchValue = "Bob",
            });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("found");
        var output = (FindRowOutput)result.OutputData!;
        output.Found.ShouldBeTrue();
        output.RowNumber.ShouldBe(3);
        output.Values.ShouldBe(["Bob", "bob@example.com"]);
    }

    [Fact]
    public async Task ExecuteAsync_returns_notFound_outcome_when_no_row_matches()
    {
        var handler = StubHandler(HttpStatusCode.OK, """
            {
              "values": [
                ["Alice", "alice@example.com"],
                ["Bob", "bob@example.com"]
              ]
            }
            """);

        var result = await BuildHarness(handler,
            new FindRowSettings
            {
                SpreadsheetId = "SHEET_ID",
                SheetName = "Sheet1",
                SearchColumn = "A",
                SearchValue = "Charlie",
            });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
        var output = (FindRowOutput)result.OutputData!;
        output.Found.ShouldBeFalse();
        output.RowNumber.ShouldBe(0);
        output.Values.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_returns_notFound_when_sheet_is_empty()
    {
        var handler = StubHandler(HttpStatusCode.OK, """{}""");

        var result = await BuildHarness(handler,
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "X" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
    }

    [Fact]
    public async Task ExecuteAsync_handles_jagged_rows_without_throwing()
    {
        // Row 2 has only one value — searching column B should treat it as empty rather than throw.
        var handler = StubHandler(HttpStatusCode.OK, """
            {
              "values": [
                ["Alice", "alice@example.com"],
                ["Bob"]
              ]
            }
            """);

        var result = await BuildHarness(handler,
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "B", SearchValue = "bob@example.com" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
    }

    [Fact]
    public async Task ExecuteAsync_search_is_case_sensitive()
    {
        var handler = StubHandler(HttpStatusCode.OK, """
            {
              "values": [["alice"], ["Alice"], ["ALICE"]]
            }
            """);

        var result = await BuildHarness(handler,
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "alice" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("found");
        ((FindRowOutput)result.OutputData!).RowNumber.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_resolves_correct_api_url()
    {
        HttpRequestMessage? captured = null;
        var handler = StubHandler(HttpStatusCode.OK, """{"values":[]}""", req => captured = req);

        await BuildHarness(handler,
            new FindRowSettings { SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "x" });

        captured!.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1");
    }

    [Theory]
    [InlineData("A", 0)]
    [InlineData("B", 1)]
    [InlineData("Z", 25)]
    [InlineData("AA", 26)]
    [InlineData("AB", 27)]
    [InlineData("AZ", 51)]
    [InlineData("BA", 52)]
    public void ColumnLetterToIndex_converts_correctly(string letter, int expected) =>
        ColumnLetterParser.ToIndex(letter).ShouldBe(expected);

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new FindRowSettings { SpreadsheetId = "", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "x" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_sheet_name_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "", SearchColumn = "A", SearchValue = "x" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_search_column_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "", SearchValue = "x" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_search_column_is_not_a_letter()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "1", SearchValue = "x" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("not a valid column letter");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_url_is_unrelated_site()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new FindRowSettings { SpreadsheetId = "https://www.dropbox.com/s/abc/file.xlsx", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "x" });

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

        var result = await ActionTestHarness.For<FindRowAction>()
            .WithService(CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}")))
            .WithService(creds.Object)
            .WithSettings(new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "x" })
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
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "x" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_rate_limited_with_friendly_message_when_quota_exceeded()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.TooManyRequests,
                """{"error":{"code":429,"message":"Quota exceeded","status":"RESOURCE_EXHAUSTED"}}"""),
            new FindRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", SearchColumn = "A", SearchValue = "x" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.RateLimiting);
        result.Exception!.Message.ShouldContain("rate-limiting");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, FindRowSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<FindRowAction>()
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
