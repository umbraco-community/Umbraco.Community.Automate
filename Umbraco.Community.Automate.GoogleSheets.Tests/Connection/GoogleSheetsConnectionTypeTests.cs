using Moq;
using Shouldly;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.OpenIddict.Credentials;
using Umbraco.Community.Automate.GoogleSheets.Connection;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests.Connection;

public class GoogleSheetsConnectionTypeTests
{
    [Fact]
    public void ProviderName_is_Google()
    {
        var sut = CreateSut(out _);
        sut.ProviderName.ShouldBe("Google");
    }

    [Fact]
    public async Task ValidateAsync_fails_when_no_credential_id()
    {
        var sut = CreateSut(out _);
        var result = await sut.ValidateAsync(new GoogleSheetsConnectionSettings(), CancellationToken.None);
        result.Status.ShouldBe(ConnectionValidationStatus.Failure);
    }

    [Fact]
    public async Task ValidateAsync_succeeds_when_credential_id_resolves_a_valid_token()
    {
        var credentialId = Guid.NewGuid();
        var sut = CreateSut(out var creds);
        creds.Setup(c => c.GetValidAccessTokenAsync(credentialId, It.IsAny<CancellationToken>()))
             .ReturnsAsync("access-token");

        var result = await sut.ValidateAsync(
            new GoogleSheetsConnectionSettings { OAuthCredentialsId = credentialId },
            CancellationToken.None);

        result.Status.ShouldBe(ConnectionValidationStatus.Success);
    }

    private static GoogleSheetsConnectionType CreateSut(out Mock<IOAuthCredentialsService> creds)
    {
        creds = new Mock<IOAuthCredentialsService>();
        var infrastructure = new ConnectionTypeInfrastructure(Mock.Of<IEditableModelResolver>());
        return new GoogleSheetsConnectionType(infrastructure, creds.Object);
    }
}
