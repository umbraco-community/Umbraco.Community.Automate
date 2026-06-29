using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

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

        if (string.IsNullOrWhiteSpace(settings.SpreadsheetId))
            return ActionResult.Failed(new ArgumentException("Spreadsheet is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.SheetTitle))
            return ActionResult.Failed(new ArgumentException("Sheet tab title is required."), StepRunErrorCategory.Validation);

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
