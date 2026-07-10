using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;

namespace Umbraco.Community.Automate.GoogleSheets.Actions;

/// <summary>
/// Clears all values in a Google Sheet tab or a specific A1 range, preserving
/// cell formatting and row structure.
/// </summary>
[Action("googleSheets.clearRange", "Clear Range in Google Sheet",
    Description = "Clears all values in a sheet tab or a specific A1 range. Formatting is preserved.",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    ConnectionTypeAlias = "googleSheets")]
public sealed class ClearRangeAction : ActionBase<ClearRangeSettings, ClearRangeOutput>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthCredentialsService _credentialsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearRangeAction"/> class.
    /// </summary>
    public ClearRangeAction(
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
        var settings = context.GetSettings<ClearRangeSettings>();

        if (string.IsNullOrWhiteSpace(settings.SpreadsheetId))
            return ActionResult.Failed(new ArgumentException("Spreadsheet is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

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

        var rangeSegment = string.IsNullOrWhiteSpace(settings.Range)
            ? settings.SheetName
            : $"{settings.SheetName}!{settings.Range.Trim()}";

        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(rangeSegment)}:clear";

        using var client = _httpClientFactory.CreateClient("UmbracoAutomate");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // The :clear endpoint requires a POST with an empty body.
            using var response = await client.PostAsync(url, content: null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var (message, category) = GoogleApiErrorParser.Parse((int)response.StatusCode, error);
                return ActionResult.Failed(new InvalidOperationException(message), category);
            }

            var parsed = await response.Content.ReadFromJsonAsync<ClearResponse>(cancellationToken);

            return Success(new ClearRangeOutput { ClearedRange = parsed?.ClearedRange });
        }
        catch (Exception ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.InvalidResponse);
        }
    }

    private sealed record ClearResponse(
        [property: JsonPropertyName("clearedRange")] string? ClearedRange);
}
