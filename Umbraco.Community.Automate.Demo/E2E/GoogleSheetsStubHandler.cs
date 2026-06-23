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
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host != "sheets.googleapis.com")
        {
            return base.SendAsync(request, cancellationToken);
        }

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
