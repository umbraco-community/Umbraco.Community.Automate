using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Reads rows from a Google Sheet tab, optionally restricted to an A1 range.
/// </summary>
[Action("googleSheets.getRows", "Get Rows from Google Sheet",
    Description = "Reads rows from a sheet tab or a specific range.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class GetRowsAction : ActionBase<GetRowsSettings, GetRowsOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRowsAction"/> class.
    /// </summary>
    public GetRowsAction(
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
        var settings = context.GetSettings<GetRowsSettings>();

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);

        // Build the range segment: "SheetName" for the whole sheet, or "SheetName!A2:D50" for a specific range.
        var rangeSegment = string.IsNullOrWhiteSpace(settings.Range)
            ? settings.SheetName
            : $"{settings.SheetName}!{settings.Range.Trim()}";

        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(rangeSegment)}";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(response, cancellationToken) is { } responseError)
                return responseError;

            var parsed = await response.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);
            var allRows = parsed?.Values ?? [];

            List<string> headers = [];
            List<List<string>> dataRows;

            if (settings.HasHeaderRow && allRows.Count > 0)
            {
                headers = allRows[0];
                dataRows = allRows.Skip(1).ToList();
            }
            else
            {
                dataRows = allRows;
            }

            return Success(new GetRowsOutput
            {
                Rows = dataRows,
                RowCount = dataRows.Count,
                Headers = headers,
            });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
