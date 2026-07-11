using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Turns a Google API error response body into a human-readable message and the
/// <see cref="StepRunErrorCategory"/> that best fits it, instead of surfacing the raw JSON.
/// Shared across every action that calls the Google Sheets API, since they all hit the same
/// error envelope shape.
/// </summary>
public static class GoogleApiErrorParser
{
    /// <summary>
    /// Checks an HTTP response for failure and, if it failed, reads the body and returns the
    /// <see cref="ActionResult"/> to return from the calling action. Returns <c>null</c> on success.
    /// </summary>
    public static async Task<ActionResult?> TryHandleErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var (message, category) = Parse((int)response.StatusCode, body);
        return ActionResult.Failed(new InvalidOperationException(message), category);
    }

    public static (string Message, StepRunErrorCategory Category) Parse(int statusCode, string body)
    {
        var status = TryGetStatus(body);
        return status switch
        {
            // The token is valid — the connected account just isn't on the file's ACL. Framing
            // this as Authentication would wrongly suggest reconnecting Google fixes it.
            "PERMISSION_DENIED" => (
                "This Google account doesn't have access to that spreadsheet. Make sure the " +
                "spreadsheet has been shared with the account used by this connection, or switch " +
                "to a connection authenticated with an account that does have access.",
                StepRunErrorCategory.InvalidResponse),

            // Google's own API design checks permission before existence (see
            // https://google.aip.dev/193), so a sheet that exists but isn't shared with the
            // caller should surface as PERMISSION_DENIED above, not this. Confirmed NOT_FOUND
            // is what a genuinely wrong/deleted spreadsheet ID returns. Real-world reports (e.g.
            // various automation tools' support threads) do show this same message appearing for
            // access-flavored problems too, so the hint below is phrased as a possibility to
            // check, not a confident claim about why.
            "NOT_FOUND" => (
                "Google couldn't find a spreadsheet at that URL or ID. Double-check it's correct " +
                "— if it looks right, also confirm the spreadsheet has been shared with the " +
                "connected Google account, since access problems can sometimes surface this same " +
                "error.",
                StepRunErrorCategory.Validation),

            // Real-world testing shows Google also returns INVALID_ARGUMENT when a personal
            // Google account tries to access a Google Workspace spreadsheet it has no permission
            // to (cross-domain access). The primary cause is still a wrong ID/tab name, but the
            // message includes the access-denied possibility so users don't chase their config
            // when the real fix is sharing the sheet.
            "INVALID_ARGUMENT" => (
                "Google rejected the request — this usually means the sheet/tab name doesn't " +
                "match exactly (check capitalisation), or the spreadsheet URL/ID is wrong. If " +
                "both look correct, the connected Google account may not have permission to " +
                "access this spreadsheet.",
                StepRunErrorCategory.Validation),

            // Documented at https://developers.google.com/workspace/sheets/api/limits — the
            // engine has a dedicated category for this rather than treating it as a hard failure.
            "RESOURCE_EXHAUSTED" => (
                "Google is rate-limiting requests to the Sheets API right now. This will need to " +
                "be retried after a short wait.",
                StepRunErrorCategory.RateLimiting),

            _ => ($"Google Sheets API error ({statusCode}): {body}", StepRunErrorCategory.InvalidResponse),
        };
    }

    private static string? TryGetStatus(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorEnvelope>(body)?.Error?.Status;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ErrorEnvelope([property: JsonPropertyName("error")] ErrorDetail? Error);

    private sealed record ErrorDetail([property: JsonPropertyName("status")] string? Status);
}
