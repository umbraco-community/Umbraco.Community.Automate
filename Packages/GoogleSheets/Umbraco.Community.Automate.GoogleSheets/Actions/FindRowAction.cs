using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

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

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.SearchColumn))
            return ActionResult.Failed(new ArgumentException("Search column is required."), StepRunErrorCategory.Validation);

        if (!ColumnLetterParser.IsValid(settings.SearchColumn))
            return ActionResult.Failed(
                new ArgumentException($"'{settings.SearchColumn}' is not a valid column letter (e.g. A, B, AA)."),
                StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(response, cancellationToken) is { } getError)
                return getError;

            var parsed = await response.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);
            var rows = parsed?.Values ?? [];
            var columnIndex = ColumnLetterParser.ToIndex(settings.SearchColumn);

            var comparison = settings.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var matchMode = Enum.TryParse<FindRowMatchMode>(settings.MatchMode, ignoreCase: true, out var m)
                ? m : FindRowMatchMode.Exact;

            var matchedRow = RowMatcher.FindRowIndex(
                rows, columnIndex, settings.SearchValue, comparison, matchMode, settings.HasHeaderRow);

            if (matchedRow < 0)
                return SuccessWithOutcome("notFound", new FindRowOutput { Found = false, RowNumber = 0, Values = [] });

            var output = new FindRowOutput
            {
                Found = true,
                RowNumber = matchedRow + 1,
                Values = [..rows[matchedRow]],
            };
            return SuccessWithOutcome("found", output);
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
