using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Creates a new Google Sheets spreadsheet and returns its ID and URL.
/// </summary>
[Action("googleSheets.createSpreadsheet", "Create Google Spreadsheet",
    Description = "Creates a new Google Sheets spreadsheet with an optional set of named sheet tabs. " +
                  "A new document is created every time this step runs — it does not check for an existing spreadsheet with the same title. " +
                  "To avoid duplicates, add a condition before this step that skips it when a spreadsheet ID is already stored.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class CreateSpreadsheetAction : ActionBase<CreateSpreadsheetSettings, CreateSpreadsheetOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSpreadsheetAction"/> class.
    /// </summary>
    public CreateSpreadsheetAction(
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
        var settings = context.GetSettings<CreateSpreadsheetSettings>();

        if (string.IsNullOrWhiteSpace(settings.Title))
            return ActionResult.Failed(new ArgumentException("Title is required."), StepRunErrorCategory.Validation);

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

        var sheetTitles = ParseSheetNames(settings.SheetNames);

        // Build the request body. The "sheets" array is omitted entirely when no tab names
        // are provided — Google will create a single default "Sheet1" in that case.
        object payload = sheetTitles.Count > 0
            ? new
            {
                properties = new { title = settings.Title },
                sheets = sheetTitles.Select(t => new { properties = new { title = t } }).ToArray(),
            }
            : new
            {
                properties = new { title = settings.Title },
            };

        var url = "https://sheets.googleapis.com/v4/spreadsheets";

        using var client = _httpClientFactory.CreateClient("UmbracoAutomate");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)response.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

            var parsed = await response.Content.ReadFromJsonAsync<SpreadsheetResponse>(cancellationToken);

            if (string.IsNullOrEmpty(parsed?.SpreadsheetId))
                return ActionResult.Failed(
                    new InvalidOperationException("Google returned a success response but no spreadsheet ID."),
                    StepRunErrorCategory.InvalidResponse);

            return Success(new CreateSpreadsheetOutput
            {
                SpreadsheetId = parsed.SpreadsheetId,
                SpreadsheetUrl = parsed.SpreadsheetUrl ?? $"https://docs.google.com/spreadsheets/d/{parsed.SpreadsheetId}/edit",
            });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    // Splits a comma-separated string into trimmed, non-empty tab names.
    public static List<string> ParseSheetNames(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? []
            : input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .ToList();

    private sealed record SpreadsheetResponse(
        [property: JsonPropertyName("spreadsheetId")] string? SpreadsheetId,
        [property: JsonPropertyName("spreadsheetUrl")] string? SpreadsheetUrl);
}
