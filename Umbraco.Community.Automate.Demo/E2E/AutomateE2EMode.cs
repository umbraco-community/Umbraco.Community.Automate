namespace Umbraco.Community.Automate.Demo.E2E;

/// <summary>
/// Gate for this Demo site's end-to-end test support (the Google Sheets stub HTTP handler and
/// the credential-seeding endpoint). Both are inert unless <c>AUTOMATE_E2E_MODE=1</c> is set, so
/// normal/production runs of this site are unaffected.
/// </summary>
public static class AutomateE2EMode
{
    private const string EnvironmentVariableName = "AUTOMATE_E2E_MODE";

    /// <summary>
    /// Gets a value indicating whether end-to-end test support is enabled for this process.
    /// </summary>
    public static bool IsEnabled => Environment.GetEnvironmentVariable(EnvironmentVariableName) == "1";
}
