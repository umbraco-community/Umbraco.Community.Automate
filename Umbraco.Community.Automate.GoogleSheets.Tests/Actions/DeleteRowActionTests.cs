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

public class DeleteRowActionTests
{
    private const string SheetData = """
        {
          "values": [
            ["Name", "Email"],
            ["Alice", "alice@example.com"],
            ["Bob", "bob@example.com"]
          ]
        }
        """;

    private const string MetaData = """
        {
          "sheets": [
            { "properties": { "sheetId": 42, "title": "Sheet1" } }
          ]
        }
        """;

    private const string BatchOk = """{"spreadsheetId":"SHEET_ID","replies":[{}]}""";

    [Fact]
    public async Task ExecuteAsync_returns_deleted_outcome_and_issues_three_api_calls()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = SequenceHandler([SheetData, MetaData, BatchOk], req => requests.Add(req));

        var result = await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "Bob",
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("deleted");

        requests.Count.ShouldBe(3);
        requests[0].Method.ShouldBe(HttpMethod.Get);
        requests[0].RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1");
        requests[1].Method.ShouldBe(HttpMethod.Get);
        requests[1].RequestUri!.ToString()
            .ShouldContain("?fields=sheets.properties");
        requests[2].Method.ShouldBe(HttpMethod.Post);
        requests[2].RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID:batchUpdate");

        var output = (DeleteRowOutput)result.OutputData!;
        output.DeletedRowNumber.ShouldBe(3); // Bob is row 3 (1-based)
    }

    [Fact]
    public async Task ExecuteAsync_sends_correct_delete_dimension_payload_for_matched_row()
    {
        string? batchBody = null;
        var handler = SequenceHandler([SheetData, MetaData, BatchOk], req =>
        {
            if (req.Method == HttpMethod.Post)
                batchBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        });

        await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "Alice", // row index 1 (0-based), row 2 (1-based)
        });

        batchBody.ShouldNotBeNull();
        batchBody.ShouldContain("\"deleteDimension\"");
        batchBody.ShouldContain("\"sheetId\":42");
        batchBody.ShouldContain("\"startIndex\":1");
        batchBody.ShouldContain("\"endIndex\":2");
        batchBody.ShouldContain("\"ROWS\"");
    }

    [Fact]
    public async Task ExecuteAsync_returns_notFound_when_no_row_matches()
    {
        var handler = SequenceHandler([SheetData, MetaData, BatchOk]);

        var result = await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "Charlie",
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
        ((DeleteRowOutput)result.OutputData!).DeletedRowNumber.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_returns_notFound_when_sheet_is_empty()
    {
        var handler = SequenceHandler(["""{}""", MetaData, BatchOk]);

        var result = await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            LookupColumn = "A",
            LookupValue = "X",
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("notFound");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(SequenceHandler(["{}","{}","{}"]), new DeleteRowSettings
        {
            SpreadsheetId = "", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x",
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_lookup_column_is_not_a_letter()
    {
        var result = await BuildHarness(SequenceHandler(["{}","{}","{}"]), new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A2", LookupValue = "x",
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("not a valid column letter");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_lookup_value_missing()
    {
        var result = await BuildHarness(SequenceHandler(["{}","{}","{}"]), new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "",
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

        var result = await ActionTestHarness.For<DeleteRowAction>()
            .WithService(CreateHttpClientFactory(SequenceHandler(["{}","{}","{}"])))
            .WithService(creds.Object)
            .WithSettings(new DeleteRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x" })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_permission_denied()
    {
        var handler = SingleCallHandler(HttpStatusCode.Forbidden,
            """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""");

        var result = await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "x",
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_metadata_permission_denied()
    {
        var permissionDenied = """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""";
        var handler = SequenceHandler(
            [SheetData, permissionDenied, "{}"],
            statusCodes: [HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.OK]);

        var result = await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "Bob",
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_on_batchUpdate_permission_denied()
    {
        var permissionDenied = """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""";
        var handler = SequenceHandler(
            [SheetData, MetaData, permissionDenied],
            statusCodes: [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.Forbidden]);

        var result = await BuildHarness(handler, new DeleteRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", LookupColumn = "A", LookupValue = "Bob",
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, DeleteRowSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<DeleteRowAction>()
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

    // Cycles through responses (and optional per-call status codes) in order, one per successive call.
    private static HttpMessageHandler SequenceHandler(
        IReadOnlyList<string> responses, Action<HttpRequestMessage>? capture = null, IReadOnlyList<HttpStatusCode>? statusCodes = null)
    {
        var callCount = 0;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture?.Invoke(req);
                var index = callCount++;
                var json = index < responses.Count ? responses[index] : "{}";
                var status = statusCodes is not null && index < statusCodes.Count ? statusCodes[index] : HttpStatusCode.OK;
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
