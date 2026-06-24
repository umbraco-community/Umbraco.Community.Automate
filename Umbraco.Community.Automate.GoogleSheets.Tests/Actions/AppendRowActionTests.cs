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

public class AppendRowActionTests
{
    [Fact]
    public async Task ExecuteAsync_posts_append_request_and_maps_output()
    {
        HttpRequestMessage? captured = null;
        var handler = StubHandler(HttpStatusCode.OK,
            """{"updates":{"updatedRange":"Sheet1!A2:B2","updatedRows":1,"updatedCells":2}}""",
            req => captured = req);
        var httpClientFactory = CreateHttpClientFactory(handler);

        var credentialId = Guid.NewGuid();
        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings
            {
                SpreadsheetId = "https://docs.google.com/spreadsheets/d/SHEET_ID/edit",
                SheetName = "Sheet1",
                Columns = ["alice", "alice@example.com"],
            })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = credentialId })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        captured!.RequestUri!.ToString()
            .ShouldBe("https://sheets.googleapis.com/v4/spreadsheets/SHEET_ID/values/Sheet1:append?valueInputOption=USER_ENTERED");
        var body = await captured.Content!.ReadAsStringAsync();
        body.ShouldContain("\"alice@example.com\"");
        var output = (AppendRowOutput)result.OutputData!;
        output.UpdatedRows.ShouldBe(1);
        output.UpdatedCells.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_when_spreadsheet_missing()
    {
        var httpClientFactory = CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}", _ => { }));

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(Mock.Of<IOAuthCredentialsService>())
            .WithSettings(new AppendRowSettings { SpreadsheetId = "", SheetName = "Sheet1" })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_fails_authentication_when_token_null()
    {
        var httpClientFactory = CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}", _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task ExecuteAsync_fails_with_friendly_message_when_caller_lacks_permission()
    {
        var httpClientFactory = CreateHttpClientFactory(
            StubHandler(HttpStatusCode.Forbidden,
                """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""",
                _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
        result.Exception!.Message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_with_friendly_message_when_spreadsheet_not_found()
    {
        var httpClientFactory = CreateHttpClientFactory(
            StubHandler(HttpStatusCode.NotFound,
                """{"error":{"code":404,"message":"Requested entity was not found.","status":"NOT_FOUND"}}""",
                _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "DOES_NOT_EXIST", SheetName = "Sheet1", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("couldn't find a spreadsheet");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_with_friendly_message_when_argument_invalid()
    {
        var httpClientFactory = CreateHttpClientFactory(
            StubHandler(HttpStatusCode.BadRequest,
                """{"error":{"code":400,"message":"Invalid range.","status":"INVALID_ARGUMENT"}}""",
                _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Not A Real Tab", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("rejected the spreadsheet ID or sheet name");
    }

    [Fact]
    public async Task ExecuteAsync_fails_validation_without_calling_api_when_link_is_unrelated_site()
    {
        var callCount = 0;
        var httpClientFactory = CreateHttpClientFactory(
            StubHandler(HttpStatusCode.OK, "{}", _ => callCount++));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings
            {
                SpreadsheetId = "https://www.dropbox.com/s/abc123/notes.xlsx",
                SheetName = "Sheet1",
                Columns = ["x"],
            })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("doesn't look like a Google Sheets link");
        callCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_succeeds_with_zeroed_output_when_response_body_omits_updates()
    {
        // Google's success responses always include "updates", but the deserialization is
        // defensive about it (Updates is nullable) — this documents that a 200 with no
        // "updates" object degrades to a zeroed output rather than throwing.
        var httpClientFactory = CreateHttpClientFactory(StubHandler(HttpStatusCode.OK, "{}", _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (AppendRowOutput)result.OutputData!;
        output.UpdatedRange.ShouldBeNull();
        output.UpdatedRows.ShouldBe(0);
        output.UpdatedCells.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_fails_invalid_response_when_body_is_not_json()
    {
        var httpClientFactory = CreateHttpClientFactory(
            StubHandler(HttpStatusCode.OK, "<html>not json</html>", _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
    }

    [Fact]
    public async Task ExecuteAsync_fails_invalid_response_when_body_is_truncated_json()
    {
        var httpClientFactory = CreateHttpClientFactory(
            StubHandler(HttpStatusCode.OK, """{"updates":{"updatedRange":"Sheet1!A2:B2""", _ => { }));

        var creds = new Mock<IOAuthCredentialsService>();
        creds.Setup(c => c.GetValidAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("tok123");

        var result = await ActionTestHarness.For<AppendRowAction>()
            .WithService(httpClientFactory)
            .WithService(creds.Object)
            .WithSettings(new AppendRowSettings { SpreadsheetId = "SHEET_ID", SheetName = "Sheet1", Columns = ["x"] })
            .WithConnection("googleSheets", new GoogleSheetsConnectionSettings { OAuthCredentialsId = Guid.NewGuid() })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.InvalidResponse);
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UmbracoAutomate")).Returns(client);
        return factory.Object;
    }

    private static HttpMessageHandler StubHandler(HttpStatusCode code, string json, Action<HttpRequestMessage> capture)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capture(req);
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            });
        return mock.Object;
    }
}
