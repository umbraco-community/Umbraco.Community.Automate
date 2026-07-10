using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Searches a Google Sheet for a row matching the key column value.
/// Updates the row if found; appends a new row if not. Produces "updated" or "appended" outcomes.
/// </summary>
[Action("googleSheets.appendOrUpdateRow", "Append or Update Row in Google Sheet",
    Description = "Updates a row if a matching key column value is found, otherwise appends a new row.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class AppendOrUpdateRowAction : ActionBase<AppendOrUpdateRowSettings, AppendOrUpdateRowOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendOrUpdateRowAction"/> class.
    /// </summary>
    public AppendOrUpdateRowAction(
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
        var settings = context.GetSettings<AppendOrUpdateRowSettings>();

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.KeyColumn))
            return ActionResult.Failed(new ArgumentException("Key column is required."), StepRunErrorCategory.Validation);

        if (!ColumnLetterParser.IsValid(settings.KeyColumn))
            return ActionResult.Failed(
                new ArgumentException($"'{settings.KeyColumn}' is not a valid column letter (e.g. A, B, AA)."),
                StepRunErrorCategory.Validation);

        if (settings.Columns is not { Count: > 0 })
            return ActionResult.Failed(new ArgumentException("At least one column value is required."), StepRunErrorCategory.Validation);

        var keyColumnIndex = ColumnLetterParser.ToIndex(settings.KeyColumn);
        if (keyColumnIndex >= settings.Columns.Count)
            return ActionResult.Failed(
                new ArgumentException(
                    $"Key column '{settings.KeyColumn}' is outside the {settings.Columns.Count} column value(s) provided. " +
                    "Include a value for the key column among the column values."),
                StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);
        // Derive the key value from the column index into the provided columns list.
        // Bounds-validated above, so keyColumnIndex is always a valid index into Columns here.
        var keyValue = settings.Columns[keyColumnIndex];

        try
        {
            // Step 1: fetch all rows to check whether the key value already exists.
            var getUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}";
            using var getResponse = await httpClient.GetAsync(getUrl, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(getResponse, cancellationToken) is { } getError)
                return getError;

            var parsed = await getResponse.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);
            var rows = parsed?.Values ?? [];

            var matchedRow = RowMatcher.FindRowIndex(
                rows, keyColumnIndex, keyValue, StringComparison.Ordinal, hasHeaderRow: settings.HasHeaderRow);

            if (matchedRow >= 0)
            {
                // Step 2a: update the existing row in-place.
                var rowNumber = matchedRow + 1;
                var updateRange = $"{settings.SheetName}!A{rowNumber}";
                var putUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(updateRange)}?valueInputOption=USER_ENTERED";
                var putPayload = new { values = new[] { settings.Columns.ToArray() } };

                using var putResponse = await httpClient.PutAsJsonAsync(putUrl, putPayload, cancellationToken);
                if (await GoogleApiErrorParser.TryHandleErrorAsync(putResponse, cancellationToken) is { } putError)
                    return putError;

                var updateResult = await putResponse.Content.ReadFromJsonAsync<WriteValuesResponse>(cancellationToken);
                return SuccessWithOutcome("updated", new AppendOrUpdateRowOutput
                {
                    RowNumber = rowNumber,
                    UpdatedRange = updateResult?.UpdatedRange,
                    UpdatedRows = updateResult?.UpdatedRows ?? 0,
                    UpdatedCells = updateResult?.UpdatedCells ?? 0,
                });
            }
            else
            {
                // Step 2b: append a new row.
                var appendUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}:append?valueInputOption=USER_ENTERED";
                var appendPayload = new { values = new[] { settings.Columns.ToArray() } };

                using var appendResponse = await httpClient.PostAsJsonAsync(appendUrl, appendPayload, cancellationToken);
                if (await GoogleApiErrorParser.TryHandleErrorAsync(appendResponse, cancellationToken) is { } appendError)
                    return appendError;

                var appendResult = await appendResponse.Content.ReadFromJsonAsync<AppendApiResponse>(cancellationToken);
                var updates = appendResult?.Updates;

                // Derive the 1-based row number from the updated range (e.g. "Sheet1!A5:C5" → 5).
                var appendedRow = ParseRowNumberFromRange(updates?.UpdatedRange);
                return SuccessWithOutcome("appended", new AppendOrUpdateRowOutput
                {
                    RowNumber = appendedRow,
                    UpdatedRange = updates?.UpdatedRange,
                    UpdatedRows = updates?.UpdatedRows ?? 0,
                    UpdatedCells = updates?.UpdatedCells ?? 0,
                });
            }
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    // Parses the starting row number from an A1 range like "Sheet1!A5:C5" or "A5:C5". Returns 0 if unparseable.
    private static int ParseRowNumberFromRange(string? range)
    {
        if (string.IsNullOrEmpty(range)) return 0;
        var bang = range.IndexOf('!');
        var cell = bang >= 0 ? range[(bang + 1)..] : range;
        var digits = new string(cell.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : 0;
    }

    private sealed record WriteValuesResponse(
        [property: JsonPropertyName("updatedRange")] string? UpdatedRange,
        [property: JsonPropertyName("updatedRows")] int UpdatedRows,
        [property: JsonPropertyName("updatedCells")] int UpdatedCells);

    private sealed record AppendApiResponse(
        [property: JsonPropertyName("updates")] WriteValuesResponse? Updates);
}
