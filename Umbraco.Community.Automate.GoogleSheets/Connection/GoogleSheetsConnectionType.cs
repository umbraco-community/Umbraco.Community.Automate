using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.OpenIddict.ConnectionTypes;
using Umbraco.Automate.OpenIddict.Credentials;

namespace Umbraco.Community.Automate.GoogleSheets.Connection;

/// <summary>
/// Connection type for Google Sheets using OAuth via OpenIddict WebIntegration.
/// </summary>
[ConnectionType("googleSheets", "Google Sheets",
    Group = "Productivity",
    Icon = "icon-google-sheets",
    Description = "Connect to Google Sheets")]
public sealed class GoogleSheetsConnectionType : OAuthConnectionTypeBase<GoogleSheetsConnectionSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSheetsConnectionType"/> class.
    /// </summary>
    public GoogleSheetsConnectionType(
        ConnectionTypeInfrastructure infrastructure,
        IOAuthCredentialsService credentialsService)
        : base(infrastructure, credentialsService)
    {
    }

    /// <inheritdoc />
    public override string ProviderName => "GoogleSheets";
}
