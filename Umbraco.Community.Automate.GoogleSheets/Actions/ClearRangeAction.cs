using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.OpenIddict.Credentials;

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

        if (SpreadsheetIdParser.ValidateSpreadsheetId(settings.SpreadsheetId) is { } spreadsheetIdError)
            return spreadsheetIdError;

        if (string.IsNullOrWhiteSpace(settings.SheetName))
            return ActionResult.Failed(new ArgumentException("Sheet name is required."), StepRunErrorCategory.Validation);

        var (client, authError) = await GoogleSheetsAuth.AuthenticateAsync(context, _httpClientFactory, _credentialsService, cancellationToken);
        if (authError is not null)
            return authError;
        using var httpClient = client!;

        var spreadsheetId = SpreadsheetIdParser.Parse(settings.SpreadsheetId);

        var rangeSegment = string.IsNullOrWhiteSpace(settings.Range)
            ? settings.SheetName
            : $"{settings.SheetName}!{settings.Range.Trim()}";

        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(rangeSegment)}:clear";

        try
        {
            // The :clear endpoint requires a POST with an empty body.
            using var response = await httpClient.PostAsync(url, content: null, cancellationToken);
            if (await GoogleApiErrorParser.TryHandleErrorAsync(response, cancellationToken) is { } responseError)
                return responseError;

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
