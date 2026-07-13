using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Finds the first row in a Google Sheet where a column matches a value, then overwrites
/// that row's column values. Produces an "updated" or "notFound" outcome for branching.
/// </summary>
[Action("googleSheets.updateRow", "Update Row in Google Sheet",
    Description = "Finds a row by column value and updates its column values.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class UpdateRowAction : ActionBase<UpdateRowSettings, UpdateRowOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRowAction"/> class.
    /// </summary>
    public UpdateRowAction(
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
        var settings = context.GetSettings<UpdateRowSettings>();

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

        if (settings.Columns is not { Count: > 0 })
            return ActionResult.Failed(new ArgumentException("At least one column value is required."), StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);

        try
        {
            // Step 1: fetch all rows to locate the target row by column value.
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
                return SuccessWithOutcome("notFound", new UpdateRowOutput { RowNumber = 0 });

            // Step 2: overwrite the matched row. Specifying the top-left cell is sufficient —
            // the API extends the range rightward to match the values array length.
            var rowNumber = matchedRow + 1;
            var updateRange = $"{settings.SheetName}!A{rowNumber}";
            var putUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(updateRange)}?valueInputOption=USER_ENTERED";
            var payload = new { values = new[] { settings.Columns.ToArray() } };

            using var putResponse = await httpClient.PutAsJsonAsync(putUrl, payload, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(putResponse, cancellationToken) is { } putError)
                return putError;

            var updateResult = await putResponse.Content.ReadFromJsonAsync<UpdateValuesResponse>(cancellationToken);
            return SuccessWithOutcome("updated", new UpdateRowOutput
            {
                RowNumber = rowNumber,
                UpdatedRange = updateResult?.UpdatedRange,
                UpdatedRows = updateResult?.UpdatedRows ?? 0,
                UpdatedCells = updateResult?.UpdatedCells ?? 0,
            });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    private sealed record UpdateValuesResponse(
        [property: JsonPropertyName("updatedRange")] string? UpdatedRange,
        [property: JsonPropertyName("updatedRows")] int UpdatedRows,
        [property: JsonPropertyName("updatedCells")] int UpdatedCells);
}
