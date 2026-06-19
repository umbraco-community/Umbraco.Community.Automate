using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Appends a row of values to a Google Sheet using the Sheets API.
/// Requires a Google Sheets connection with the <c>https://www.googleapis.com/auth/spreadsheets</c> scope.
/// </summary>
[Action("googleSheets.appendRow", "Append Row to Google Sheet",
    Description = "Appends a row of values to a Google Sheet.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class AppendRowAction : ActionBase<AppendRowSettings, AppendRowOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendRowAction"/> class.
    /// </summary>
    public AppendRowAction(
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
        var settings = context.GetSettings<AppendRowSettings>();

        if (string.IsNullOrWhiteSpace(settings.SpreadsheetId))
        {
            return ActionResult.Failed(
                new ArgumentException("Spreadsheet is required."),
                StepRunErrorCategory.Validation);
        }

        if (string.IsNullOrWhiteSpace(settings.SheetName))
        {
            return ActionResult.Failed(
                new ArgumentException("Sheet name is required."),
                StepRunErrorCategory.Validation);
        }

        var connectionSettings = context.Connection?.GetSettings<GoogleSheetsConnectionSettings>();
        if (connectionSettings?.OAuthCredentialsId is not { } credentialId || credentialId == Guid.Empty)
        {
            return ActionResult.Failed(
                new InvalidOperationException("Google account is not authenticated."),
                StepRunErrorCategory.Authentication);
        }

        var token = await _credentialsService.GetValidAccessTokenAsync(credentialId, cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Google access token is expired or revoked. Reconnect the account."),
                StepRunErrorCategory.Authentication);
        }

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(settings.SheetName)}:append?valueInputOption=USER_ENTERED";
        var payload = new { values = new[] { settings.Columns.ToArray() } };

        using var client = _httpClientFactory.CreateClient("UmbracoAutomate");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return ActionResult.Failed(
                    new InvalidOperationException($"Google Sheets API error ({(int)response.StatusCode}): {error}"),
                    StepRunErrorCategory.InvalidResponse);
            }

            var parsed = await response.Content.ReadFromJsonAsync<AppendApiResponse>(cancellationToken);
            var updates = parsed?.Updates;
            return Success(new AppendRowOutput
            {
                UpdatedRange = updates?.UpdatedRange,
                UpdatedRows = updates?.UpdatedRows ?? 0,
                UpdatedCells = updates?.UpdatedCells ?? 0,
            });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    // Google's Sheets API returns camelCase JSON. System.Net.Http.Json's ReadFromJsonAsync<T>
    // uses System.Text.Json defaults (case-sensitive property matching) when no JsonSerializerOptions
    // are supplied, and the shared "UmbracoAutomate" HttpClient registration applies no JSON options.
    // Explicit [JsonPropertyName] attributes are required so the camelCase payload maps onto these
    // PascalCase records.
    private sealed record AppendApiResponse(
        [property: JsonPropertyName("updates")] UpdatesPayload? Updates);

    private sealed record UpdatesPayload(
        [property: JsonPropertyName("updatedRange")] string? UpdatedRange,
        [property: JsonPropertyName("updatedRows")] int UpdatedRows,
        [property: JsonPropertyName("updatedCells")] int UpdatedCells);
}
