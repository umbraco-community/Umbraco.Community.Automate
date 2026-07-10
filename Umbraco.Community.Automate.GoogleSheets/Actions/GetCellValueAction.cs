using System.Net.Http.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Reads the value of a single cell from a Google Sheet.
/// </summary>
[Action("googleSheets.getCellValue", "Get Cell Value from Google Sheet",
    Description = "Reads the value of a single cell by A1 notation (e.g. A1, B5).",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class GetCellValueAction : ActionBase<GetCellValueSettings, GetCellValueOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCellValueAction"/> class.
    /// </summary>
    public GetCellValueAction(
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
        var settings = context.GetSettings<GetCellValueSettings>();

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.Cell))
            return ActionResult.Failed(new ArgumentException("Cell address is required (e.g. A1)."), StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);
        var rangeSegment = $"{settings.SheetName}!{settings.Cell.Trim()}";
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(rangeSegment)}";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(response, cancellationToken) is { } responseError)
                return responseError;

            var parsed = await response.Content.ReadFromJsonAsync<ValuesResponse>(cancellationToken);

            // The API omits "values" entirely when the cell is empty.
            var cellValue = parsed?.Values?.FirstOrDefault()?.FirstOrDefault() ?? string.Empty;

            return Success(new GetCellValueOutput
            {
                Value = cellValue,
                IsEmpty = string.IsNullOrEmpty(cellValue),
            });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }
}
