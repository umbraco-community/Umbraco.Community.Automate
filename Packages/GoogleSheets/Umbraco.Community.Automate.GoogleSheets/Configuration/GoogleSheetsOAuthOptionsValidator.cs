using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Community.Automate.GoogleSheets.Configuration;

/// <summary>
/// Validates that Google Sheets OAuth credentials are present in configuration.
/// Logs a warning with actionable setup guidance before returning the validation
/// failure so the message appears in the log at warning level ahead of the
/// startup exception the options system raises.
/// </summary>
internal sealed class GoogleSheetsOAuthOptionsValidator : IValidateOptions<GoogleSheetsOAuthOptions>
{
    private readonly ILogger<GoogleSheetsOAuthOptionsValidator> _logger;

    public GoogleSheetsOAuthOptionsValidator(ILogger<GoogleSheetsOAuthOptionsValidator> logger)
        => _logger = logger;

    public ValidateOptionsResult Validate(string? name, GoogleSheetsOAuthOptions options)
    {
        var missing = new List<string>(2);

        if (string.IsNullOrEmpty(options.ClientId))
            missing.Add($"{GoogleSheetsOAuthOptions.SectionPath}:ClientId");

        if (string.IsNullOrEmpty(options.ClientSecret))
            missing.Add($"{GoogleSheetsOAuthOptions.SectionPath}:ClientSecret");

        if (missing.Count == 0)
            return ValidateOptionsResult.Success;

        _logger.LogWarning(
            "Google Sheets OAuth provider is not configured — the following configuration " +
            "keys are missing or empty: {MissingKeys}. Add a ClientId and ClientSecret from " +
            "your Google Cloud Console OAuth client to enable authentication. " +
            "See the package README for setup instructions.",
            string.Join(", ", missing));

        return ValidateOptionsResult.Fail(
            $"Google Sheets OAuth credentials are required but not configured. " +
            $"Missing: {string.Join(", ", missing)}");
    }
}
