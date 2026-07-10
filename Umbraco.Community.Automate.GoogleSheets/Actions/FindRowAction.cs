using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Searches a Google Sheet column for a matching value and returns the first matching row.
/// Produces a "found" or "notFound" outcome for conditional branching.
/// </summary>
[Action("googleSheets.findRow", "Find Row in Google Sheet",
    Description = "Searches a column for a matching value and returns the first matching row.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class FindRowAction : ActionBase<FindRowSettings, FindRowOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindRowAction"/> class.
    /// </summary>
    public FindRowAction(
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
        var settings = context.GetSettings<FindRowSettings>();

        if (string.IsNullOrWhiteSpace(settings.SpreadsheetId))
            return ActionResult.Failed(new ArgumentException("Spreadsheet is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.SearchColumn))
            return ActionResult.Failed(new ArgumentException("Search column is required."), StepRunErrorCategory.Validation);

        if (!ColumnLetterParser.IsValid(settings.SearchColumn))
            return ActionResult.Failed(
                new ArgumentException($"'{settings.SearchColumn}' is not a valid column letter (e.g. A, B, AA)."),
                StepRunErrorCategory.Validation);

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
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}";

        using var client = _httpClientFactory.CreateClient("UmbracoAutomate");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)response.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

            var parsed = await response.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);
            var rows = parsed?.Values ?? [];
            var columnIndex = ColumnLetterParser.ToIndex(settings.SearchColumn);

            var comparison = settings.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var matchMode = Enum.TryParse<FindRowMatchMode>(settings.MatchMode, ignoreCase: true, out var m)
                ? m : FindRowMatchMode.Exact;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var cellValue = columnIndex < row.Count ? row[columnIndex] : string.Empty;

                var isMatch = matchMode switch
                {
                    FindRowMatchMode.Contains   => cellValue.Contains(settings.SearchValue, comparison),
                    FindRowMatchMode.StartsWith => cellValue.StartsWith(settings.SearchValue, comparison),
                    FindRowMatchMode.EndsWith   => cellValue.EndsWith(settings.SearchValue, comparison),
                    _                           => string.Equals(cellValue, settings.SearchValue, comparison),
                };

                if (!isMatch) continue;

                var output = new FindRowOutput
                {
                    Found = true,
                    RowNumber = i + 1,
                    Values = [..row],
                };
                return SuccessWithOutcome("found", output);
            }

            return SuccessWithOutcome("notFound", new FindRowOutput { Found = false, RowNumber = 0, Values = [] });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    private sealed record ValuesResponse(
        [property: JsonPropertyName("values")] List<List<string>>? Values);
}
