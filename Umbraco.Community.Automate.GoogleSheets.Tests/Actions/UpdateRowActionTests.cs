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

public class UpdateRowActionTests
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

    private const string UpdateOk = """
        {"updatedRange":"Sheet1!A3:C3","updatedRows":1,"updatedCells":3}
        """;

    [Fact]
    public async Task ExecuteAsync_returns_updated_outcome_and_calls_get_then_put()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = TwoCallHandler(SheetData, UpdateOk, req => requests.Add(req));

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "Bob",
            Columns = ["Bob", "bob@new.com", "Active"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("updated");

        requests.Count.ShouldBe(2);
        requests[0].Method.ShouldBe(HttpMethod.Get);
        requests[0].RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1");
        requests[1].Method.ShouldBe(HttpMethod.Put);
        requests[1].RequestUri!.ToString()
            .ShouldContain("values/Sheet1%21A3?valueInputOption=USER_ENTERED");

        var output = (UpdateRowOutput)result.OutputData!;
        output.RowNumber.ShouldBe(3);
        output.UpdatedRows.ShouldBe(1);
        output.UpdatedCells.ShouldBe(3);
        output.UpdatedRange.ShouldBe("Sheet1!A3:C3");
    }

    [Fact]
    public async Task ExecuteAsync_returns_notFound_outcome_when_no_row_matches()
    {
        var handler = TwoCallHandler(SheetData, UpdateOk);

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "Charlie",
            Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
        ((UpdateRowOutput)result.OutputData!).RowNumber.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_returns_notFound_when_sheet_is_empty()
    {
        var handler = TwoCallHandler("""{}""", UpdateOk);

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "X",
            Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
    }

    [Fact]
    public async Task ExecuteAsync_default_HasHeaderRow_skips_a_header_matching_lookup_value()
    {
        var handler = TwoCallHandler(SheetData, UpdateOk);

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "Name", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
    }

    [Fact]
    public async Task ExecuteAsync_HasHeaderRow_false_allows_matching_and_overwriting_row_zero()
    {
        var handler = TwoCallHandler(SheetData, UpdateOk);

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "Name",
            Columns = ["x"], HasHeaderRow = false,
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("updated");
        ((UpdateRowOutput)result.OutputData!).RowNumber.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new UpdateRowSettings
        {
            SpreadsheetId = "", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_sheet_name_missing()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "", LookupColumn = "A", LookupValue = "x", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_lookup_column_missing()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "", LookupValue = "x", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_lookup_column_is_not_a_letter()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "1A", LookupValue = "x", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("not a valid column letter");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_lookup_value_missing()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_columns_empty()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x", Columns = [],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<UpdateRowAction>()
            .WithService(CreateHttpClientFactory(TwoCallHandler("{}", "{}")))
            .WithService(creds.Object)
            .WithSettings(new UpdateRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_get_permission_denied()
    {
        var handler = SingleCallHandler(HttpStatusCode.Forbidden,
            """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""");

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_put_permission_denied()
    {
        var handler = TwoCallHandler(SheetData,
            """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""",
            putStatusCode: HttpStatusCode.Forbidden);

        var result = await BuildHarness(handler, new UpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "Bob", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, UpdateRowSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<UpdateRowAction>()
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

    // Returns getJson for the first call (GET) and putJson for the second (PUT).
    private static HttpMessageHandler TwoCallHandler(
        string getJson, string putJson, Action<HttpRequestMessage>? capture = null, HttpStatusCode putStatusCode = HttpStatusCode.OK)
    {
        var callCount = 0;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture?.Invoke(req);
                var isGet = callCount++ == 0;
                var json = isGet ? getJson : putJson;
                var status = isGet ? HttpStatusCode.OK : putStatusCode;
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }

    private static HttpMessageHandler SingleCallHandler(HttpStatusCode code, string json)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((_, _) =>
                Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }));
        return mock.Object;
    }
}
