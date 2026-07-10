using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Finds the first row in a Google Sheet where a column matches a value, then deletes
/// that row and shifts subsequent rows up. Produces a "deleted" or "notFound" outcome.
/// </summary>
[Action("googleSheets.deleteRow", "Delete Row from Google Sheet",
    Description = "Finds a row by column value and deletes it, shifting subsequent rows up.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class DeleteRowAction : ActionBase<DeleteRowSettings, DeleteRowOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRowAction"/> class.
    /// </summary>
    public DeleteRowAction(
        ActionInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        IOAuthCredentialsService credentialsService)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _credentialsService = credentialsService;
    }

    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<DeleteRowSettings>();

        if (string.IsNullOrWhiteSpace(settings.SpreadsheetId))
            return ActionResult.Failed(new ArgumentException("Spreadsheet is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.LookupColumn))
            return ActionResult.Failed(new ArgumentException("Lookup column is required."), StepRunErrorCategory.Validation);

        if (!ColumnLetterParser.IsValid(settings.LookupColumn))
            return ActionResult.Failed(
                new ArgumentException($"'{settings.LookupColumn}' is not a valid column letter (e.g. A, B, AA)."),
                StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.LookupValue))
            return ActionResult.Failed(new ArgumentException("Lookup value is required."), StepRunErrorCategory.Validation);

        if (SpreadsheetIdParser.LooksLikeUnrelatedUrl(settings.SpreadsheetId))
            return ActionResult.Failed(
                new ArgumentException(
                    "That doesn't look like a Google Sheets link. Paste the full URL from your " +
                    "browser's address bar (e.g. https://docs.google.com/spreadsheets/d/.../edit) " +
                    "or just the spreadsheet ID."),
                StepRunErrorCategory.Validation);

        var connectionSettings = context.Connection?.GetSettings<GoogleSheetsConnectionSettings>();
        if (connectionSettings?.OAuthCredentialsId is not { } credentialId || credentialId == Guid.Empty)
            return ActionResult.Failed(
                new InvalidOperationException("Google account is not authenticated."),
                StepRunErrorCategory.Authentication);

        var token = await _credentialsService.GetValidAccessTokenAsync(credentialId, cancellationToken);
        if (string.IsNullOrEmpty(token))
            return ActionResult.Failed(
                new InvalidOperationException("Google access token is expired or revoked. Reconnect the account."),
                StepRunErrorCategory.Authentication);

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);

        using var client = _httpClientFactory.CreateClient("UmbracoAutomate");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Step 1: GET all rows to find the target row number.
            var getUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}";
            using var getResponse = await client.GetAsync(getUrl, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                var error = await getResponse.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)getResponse.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

            var parsed = await getResponse.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);
            var rows = parsed?.Values ?? [];
            var columnIndex = ColumnLetterParser.ToIndex(settings.LookupColumn);

            var matchedRow = -1;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var cellValue = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                if (string.Equals(cellValue, settings.LookupValue, StringComparison.Ordinal))
                {
                    matchedRow = i;
                    break;
                }
            }

            if (matchedRow < 0)
                return SuccessWithOutcome("notFound", new DeleteRowOutput { DeletedRowNumber = 0 });

            // Step 2: delete the row via batchUpdate with a deleteDimension request.
            // The sheetId must be fetched from spreadsheet metadata, but we can also
            // look it up from the spreadsheet properties. However, for the batchUpdate
            // deleteDimension request, we need the numeric sheetId — not the tab name.
            // To avoid a third API call, we fetch spreadsheet metadata to get the sheetId.
            var metaUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}?fields=sheets.properties";
            using var metaResponse = await client.GetAsync(metaUrl, cancellationToken);
            if (!metaResponse.IsSuccessStatusCode)
            {
                var error = await metaResponse.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)metaResponse.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

            var meta = await metaResponse.Content.ReadFromJsonAsync<SpreadsheetMetadata>(cancellationToken);
            var sheetId = meta?.Sheets?
                .FirstOrDefault(s => string.Equals(s.Properties?.Title, settings.SheetName, StringComparison.OrdinalIgnoreCase))
                ?.Properties?.SheetId;

            if (sheetId is null)
                return ActionResult.Failed(
                    new InvalidOperationException($"Sheet '{settings.SheetName}' was not found in the spreadsheet."),
                    StepRunErrorCategory.Validation);

            // batchUpdate uses 0-based row indices; matchedRow is already 0-based.
            var batchUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}:batchUpdate";
            var batchPayload = new
            {
                requests = new[]
                {
                    new
                    {
                        deleteDimension = new
                        {
                            range = new
                            {
                                sheetId = sheetId.Value,
                                dimension = "ROWS",
                                startIndex = matchedRow,
                                endIndex = matchedRow + 1,
                            },
                        },
                    },
                },
            };

            using var batchResponse = await client.PostAsJsonAsync(batchUrl, batchPayload, cancellationToken);
            if (!batchResponse.IsSuccessStatusCode)
            {
                var error = await batchResponse.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)batchResponse.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

            return SuccessWithOutcome("deleted", new DeleteRowOutput { DeletedRowNumber = matchedRow + 1 });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    private sealed record ValuesResponse(
        [property: JsonPropertyName("values")] List<List<string>>? Values);

    private sealed record SpreadsheetMetadata(
        [property: JsonPropertyName("sheets")] List<SheetEntry>? Sheets);

    private sealed record SheetEntry(
        [property: JsonPropertyName("properties")] SheetProperties? Properties);

    private sealed record SheetProperties(
        [property: JsonPropertyName("sheetId")] int SheetId,
        [property: JsonPropertyName("title")] string? Title);
}
