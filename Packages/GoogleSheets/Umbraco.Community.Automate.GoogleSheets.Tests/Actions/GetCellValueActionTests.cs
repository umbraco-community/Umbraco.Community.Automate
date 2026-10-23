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

public class GetCellValueActionTests
{
    [Fact]
    public async Task ExecuteAsync_returns_cell_value_and_not_empty()
    {
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"values":[["Hello World"]]}"""),
            new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Cell = "B2" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetCellValueOutput)result.OutputData!;
        output.Value.ShouldBe("Hello World");
        output.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_value_and_IsEmpty_true_when_cell_has_no_data()
    {
        // The API omits "values" entirely for empty cells.
        var result = await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{}"""),
            new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Cell = "A1" });

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (GetCellValueOutput)result.OutputData!;
        output.Value.ShouldBe(string.Empty);
        output.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_builds_correct_url_with_sheet_and_cell()
    {
        HttpRequestMessage? captured = null;
        await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"values":[["x"]]}""", req => captured = req),
            new GetCellValueSettings
            {
                SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
                SheetName = "Sheet1",
                Cell = "C5",
            });

        captured!.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1%21C5");
    }

    [Fact]
    public async Task ExecuteAsync_trims_whitespace_from_cell_address()
    {
        HttpRequestMessage? captured = null;
        await BuildHarness(
            StubHandler(HttpStatusCode.OK, """{"values":[["x"]]}""", req => captured = req),
            new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Cell = "  A1  " });

        captured!.RequestUri!.ToString().ShouldContain("Sheet1%21A1");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new GetCellValueSettings { SpreadsheetId = "", SheetName = "Sheet1", Cell = "A1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_sheet_name_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "", Cell = "A1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_cell_missing()
    {
        var result = await BuildHarness(StubHandler(HttpStatusCode.OK, "{}"),
            new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Cell = "" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_is_null()
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<GetCellValueAction>()
            .WithService(CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}")))
            .WithService(creds.Object)
            .WithSettings(new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Cell = "A1" })
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
            new GetCellValueSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Cell = "A1" });

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    private static Task<ActionResult> BuildHarness(HttpMessageHandler handler, GetCellValueSettings settings)
    {
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        return ActionTestHarness.For<GetCellValueAction>()
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
