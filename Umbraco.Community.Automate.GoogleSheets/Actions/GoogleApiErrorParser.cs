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

            // Google deliberately returns NOT_FOUND for both "doesn't exist" and "exists but not
            // shared with you", to avoid confirming a private file's existence — so the message
            // can't claim either with confidence.
            "NOT_FOUND" => (
                "Google couldn't find a spreadsheet at that URL or ID. Double-check it's correct " +
                "— note that Google also returns this message when a spreadsheet exists but " +
                "hasn't been shared with the connected account.",
                StepRunErrorCategory.Validation),

            "INVALID_ARGUMENT" => (
                "Google rejected the spreadsheet ID or sheet name as invalid. Double-check the " +
                "spreadsheet URL/ID and that the sheet/tab name matches exactly, including " +
                "capitalization.",
                StepRunErrorCategory.Validation),

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
