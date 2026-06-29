using System.Net;
using System.Net.Http.Json;

namespace Umbraco.Community.Automate.Demo.E2E;

/// <summary>
/// Short-circuits calls to the Google Sheets API with a canned success response, so the
/// Append Row action can be exercised end-to-end without real Google credentials or network
/// access. Registered as an outer handler on the shared "UmbracoAutomate" named <see cref="HttpClient"/>
/// only when <see cref="AutomateE2EMode.IsEnabled"/> is true — the request never reaches
/// SsrfProtectionHandler (the client's primary handler) or a socket.
/// </summary>
public sealed class GoogleSheetsStubHandler : DelegatingHandler
{
    private readonly GoogleSheetsRequestLog _requestLog;

    public GoogleSheetsStubHandler(GoogleSheetsRequestLog requestLog)
    {
        _requestLog = requestLog;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host != "sheets.googleapis.com")
        {
            return base.SendAsync(request, cancellationToken);
        }

        // The sheet name (and spreadsheet id) only ever appear in the request URL — Google's
        // append endpoint takes no body field for them — so recording the URL is enough for a
        // test to confirm a value bound from a previous step's output actually reached this call.
        _requestLog.Record(request.RequestUri.ToString());

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                updates = new
                {
                    updatedRange = "Sheet1!A1:Z1",
                    updatedRows = 1,
                    updatedCells = 1,
                },
            }),
        };

        return Task.FromResult(response);
    }
}
