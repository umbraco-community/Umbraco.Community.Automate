using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Adds a new sheet tab to an existing Google Sheets spreadsheet.
/// </summary>
[Action("googleSheets.createSheet", "Create Sheet Tab in Google Spreadsheet",
    Description = "Adds a new sheet tab to an existing spreadsheet.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class CreateSheetAction : ActionBase<CreateSheetSettings, CreateSheetOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSheetAction"/> class.
    /// </summary>
    public CreateSheetAction(
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
        var settings = context.GetSettings<CreateSheetSettings>();

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

        if (string.IsNullOrWhiteSpace(settings.SheetTitle))
            return ActionResult.Failed(new ArgumentException("Sheet tab title is required."), StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}:batchUpdate";

        var payload = new
        {
            requests = new[]
            {
                new
                {
                    addSheet = new
                    {
                        properties = new { title = settings.SheetTitle },
                    },
                },
            },
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(response, cancellationToken) is { } responseError)
                return responseError;

            var parsed = await response.Content.ReadFromJsonAsync<BatchUpdateResponse>(cancellationToken);
            var addedSheet = parsed?.Replies?.FirstOrDefault()?.AddSheet?.Properties;

            return Success(new CreateSheetOutput
            {
                SheetId = addedSheet?.SheetId ?? 0,
                SheetTitle = addedSheet?.Title ?? settings.SheetTitle,
            });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    private sealed record BatchUpdateResponse(
        [property: JsonPropertyName("replies")] List<ReplyEntry>? Replies);

    private sealed record ReplyEntry(
        [property: JsonPropertyName("addSheet")] AddSheetReply? AddSheet);

    private sealed record AddSheetReply(
        [property: JsonPropertyName("properties")] SheetProperties? Properties);

    private sealed record SheetProperties(
        [property: JsonPropertyName("sheetId")] int SheetId,
        [property: JsonPropertyName("title")] string? Title);
}
