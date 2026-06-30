using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

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

        if (settings.Columns is not { Count: > 0 })
            return ActionResult.Failed(new ArgumentException("At least one column value is required."), StepRunErrorCategory.Validation);

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
            // Step 1: fetch all rows to locate the target row by column value.
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
                return SuccessWithOutcome("notFound", new UpdateRowOutput { RowNumber = 0 });

            // Step 2: overwrite the matched row. Specifying the top-left cell is sufficient —
            // the API extends the range rightward to match the values array length.
            var rowNumber = matchedRow + 1;
            var updateRange = $"{settings.SheetName}!A{rowNumber}";
            var putUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(updateRange)}?valueInputOption=USER_ENTERED";
            var payload = new { values = new[] { settings.Columns.ToArray() } };

            using var putResponse = await client.PutAsJsonAsync(putUrl, payload, cancellationToken);
            if (!putResponse.IsSuccessStatusCode)
            {
                var error = await putResponse.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)putResponse.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

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

    private sealed record ValuesResponse(
        [property: JsonPropertyName("values")] List<List<string>>? Values);

    private sealed record UpdateValuesResponse(
        [property: JsonPropertyName("updatedRange")] string? UpdatedRange,
        [property: JsonPropertyName("updatedRows")] int UpdatedRows,
        [property: JsonPropertyName("updatedCells")] int UpdatedCells);
}
