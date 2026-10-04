using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.Mastodon.Api;

namespace Umbraco.Community.Automate.Mastodon.Connections;

[ConnectionType("community.mastodon", "Mastodon",
    Description = "Send post to Mastodon",
    Group = "Social Networks",
    Icon = "icon-automate-mastodon")]
public sealed class MastodonConnectionType : ConnectionTypeBase<MastodonSettings>
{
    private readonly MastodonClientFactory _clientFactory;

    public MastodonConnectionType(
        ConnectionTypeInfrastructure infrastructure,
        MastodonClientFactory clientFactory)
        : base(infrastructure)
    {
        _clientFactory = clientFactory;
    }

    public override async Task<ConnectionValidationResult> ValidateAsync(object? settings, CancellationToken cancellationToken)
    {
        var mastodonSettings = settings as MastodonSettings;

        var error = MastodonSettingsValidator.Validate(mastodonSettings);
        if (error is not null)
            return ConnectionValidationResult.Failure(error);

        try
        {
            using var client = _clientFactory.CreateClient(mastodonSettings!);

            using var response = await client.GetAsync(
                $"{mastodonSettings!.InstanceUrl.TrimEnd('/')}/api/v1/accounts/verify_credentials",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return ConnectionValidationResult.Failure(
                    $"Authentication failed ({(int)response.StatusCode}). Check that your access token is valid.");

            var account = await response.Content.ReadFromJsonAsync<MastodonAccountResponse>(
                cancellationToken: cancellationToken);

            return ConnectionValidationResult.Success(
                $"Connected as @{account?.Account ?? account?.Username ?? "unknown"}.");
        }
        catch (HttpRequestException ex)
        {
            return ConnectionValidationResult.Failure($"Could not reach {mastodonSettings!.InstanceUrl}: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ConnectionValidationResult.Failure("Connection timed out.");
        }
    }

    private sealed class MastodonAccountResponse
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("acct")]
        public string? Account { get; set; }
    }
}
