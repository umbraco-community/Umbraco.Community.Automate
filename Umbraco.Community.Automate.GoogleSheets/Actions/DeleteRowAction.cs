using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

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

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

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

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);

        try
        {
            // Step 1: GET all rows to find the target row number.
            var getUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}";
            using var getResponse = await httpClient.GetAsync(getUrl, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(getResponse, cancellationToken) is { } getError)
                return getError;

            var parsed = await getResponse.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);
            var rows = parsed?.Values ?? [];
            var columnIndex = ColumnLetterParser.ToIndex(settings.LookupColumn);

            var matchedRow = RowMatcher.FindRowIndex(
                rows, columnIndex, settings.LookupValue, StringComparison.Ordinal, hasHeaderRow: settings.HasHeaderRow);

            if (matchedRow < 0)
                return SuccessWithOutcome("notFound", new DeleteRowOutput { DeletedRowNumber = 0 });

            // Step 2: delete the row via batchUpdate with a deleteDimension request.
            // The sheetId must be fetched from spreadsheet metadata, but we can also
            // look it up from the spreadsheet properties. However, for the batchUpdate
            // deleteDimension request, we need the numeric sheetId — not the tab name.
            // To avoid a third API call, we fetch spreadsheet metadata to get the sheetId.
            var metaUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}?fields=sheets.properties";
            using var metaResponse = await httpClient.GetAsync(metaUrl, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(metaResponse, cancellationToken) is { } metaError)
                return metaError;

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

            using var batchResponse = await httpClient.PostAsJsonAsync(batchUrl, batchPayload, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(batchResponse, cancellationToken) is { } batchError)
                return batchError;

            return SuccessWithOutcome("deleted", new DeleteRowOutput { DeletedRowNumber = matchedRow + 1 });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    private sealed record SpreadsheetMetadata(
        [property: JsonPropertyName("sheets")] List<SheetEntry>? Sheets);

    private sealed record SheetEntry(
        [property: JsonPropertyName("properties")] SheetProperties? Properties);

    private sealed record SheetProperties(
        [property: JsonPropertyName("sheetId")] int SheetId,
        [property: JsonPropertyName("title")] string? Title);
}
