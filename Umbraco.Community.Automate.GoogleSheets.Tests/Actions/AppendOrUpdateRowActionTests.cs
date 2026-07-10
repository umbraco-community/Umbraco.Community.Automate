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

public class AppendOrUpdateRowActionTests
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

    private const string AppendOk = """
        {"updates":{"updatedRange":"Sheet1!A4:C4","updatedRows":1,"updatedCells":3}}
        """;

    [Fact]
    public async Task ExecuteAsync_returns_updated_outcome_when_key_value_found()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = TwoCallHandler(SheetData, UpdateOk, req => requests.Add(req));

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            KeyColumn = "A",
            Columns = ["Bob", "bob@new.com", "Active"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("updated");

        requests[0].Method.ShouldBe(HttpMethod.Get);
        requests[1].Method.ShouldBe(HttpMethod.Put);
        requests[1].RequestUri!.ToString().ShouldContain("Sheet1%21A3");

        var output = (AppendOrUpdateRowOutput)result.OutputData!;
        output.RowNumber.ShouldBe(3);
        output.UpdatedRows.ShouldBe(1);
        output.UpdatedCells.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_returns_appended_outcome_when_key_value_not_found()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = TwoCallHandler(SheetData, AppendOk, req => requests.Add(req));

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            KeyColumn = "A",
            Columns = ["Charlie", "charlie@example.com", "Active"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("appended");

        requests[0].Method.ShouldBe(HttpMethod.Get);
        requests[1].Method.ShouldBe(HttpMethod.Post);
        requests[1].RequestUri!.ToString().ShouldContain(":append");

        var output = (AppendOrUpdateRowOutput)result.OutputData!;
        output.RowNumber.ShouldBe(4);
        output.UpdatedRows.ShouldBe(1);
        output.UpdatedCells.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_appends_when_sheet_is_empty()
    {
        var handler = TwoCallHandler("""{}""", AppendOk);

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            KeyColumn = "A",
            Columns = ["Alice", "alice@example.com"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("appended");
    }

    [Fact]
    public async Task ExecuteAsync_default_HasHeaderRow_skips_a_header_matching_key_value()
    {
        var handler = TwoCallHandler(SheetData, AppendOk);

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A",
            Columns = ["Name", "x@example.com", "Active"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("appended");
    }

    [Fact]
    public async Task ExecuteAsync_HasHeaderRow_false_allows_matching_and_updating_row_zero()
    {
        var handler = TwoCallHandler(SheetData, UpdateOk);

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A",
            Columns = ["Name", "x@example.com", "Active"], HasHeaderRow = false,
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("updated");
        ((AppendOrUpdateRowOutput)result.OutputData!).RowNumber.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_derives_key_value_from_column_index_in_columns_list()
    {
        // KeyColumn B means index 1 — the key value should be "alice@example.com" from Columns[1].
        // The sheet has "alice@example.com" in column B row 2, so it should update row 2.
        var requests = new List<HttpRequestMessage>();
        var handler = TwoCallHandler(SheetData, UpdateOk, req => requests.Add(req));

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID",
            SheetName = "Sheet1",
            KeyColumn = "B",
            Columns = ["Alice Updated", "alice@example.com", "Inactive"],
        });

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe("updated");
        requests[1].RequestUri!.ToString().ShouldContain("Sheet1%21A2");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_key_column_missing()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_key_column_is_not_a_letter()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "1", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("not a valid column letter");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_columns_empty()
    {
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A", Columns = [],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_key_column_is_outside_columns_provided()
    {
        // KeyColumn C is index 2, but only 2 column values (indices 0-1) are provided.
        var result = await BuildHarness(TwoCallHandler("{}", "{}"), new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "C", Columns = ["a", "b"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("outside the 2 column value(s) provided");
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<AppendOrUpdateRowAction>()
            .WithService(CreateHttpClientFactory(TwoCallHandler("{}", "{}")))
            .WithService(creds.Object)
            .WithSettings(new AppendOrUpdateRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A", Columns = ["x"] })
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

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A", Columns = ["x"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_when_update_put_is_forbidden()
    {
        var permissionDenied = """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""";
        var handler = TwoCallHandler(SheetData, permissionDenied, secondStatusCode: HttpStatusCode.Forbidden);

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A", Columns = ["Bob", "bob@new.com", "Active"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_when_append_post_is_forbidden()
    {
        var permissionDenied = """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""";
        var handler = TwoCallHandler(SheetData, permissionDenied, secondStatusCode: HttpStatusCode.Forbidden);

        var result = await BuildHarness(handler, new AppendOrUpdateRowSettings
        {
            SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", KeyColumn = "A", Columns = ["Charlie", "charlie@example.com", "Active"],
        });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, AppendOrUpdateRowSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<AppendOrUpdateRowAction>()
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

    private static HttpMessageHandler TwoCallHandler(
        string firstJson, string secondJson, Action<HttpRequestMessage>? capture = null, HttpStatusCode secondStatusCode = HttpStatusCode.OK)
    {
        var callCount = 0;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture?.Invoke(req);
                var isFirst = callCount++ == 0;
                var json = isFirst ? firstJson : secondJson;
                var status = isFirst ? HttpStatusCode.OK : secondStatusCode;
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
