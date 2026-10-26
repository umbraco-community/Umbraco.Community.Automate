using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.WeatherApi.Connections;
using Umbraco.Community.Automate.WeatherApi.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Connections;

public class WeatherApiConnectionTypeTests
{
    [Fact]
    public async Task Unresolved_reference_fails_without_calling_the_api()
    {
        var handler = new StubHttpMessageHandler();

        var result = await CreateSut(handler).ValidateAsync(new WeatherApiConnectionSettings(), CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Valid_key_succeeds()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, WeatherApiResponses.Current));

        var result = await CreateSut(handler).ValidateAsync(new WeatherApiConnectionSettings { ApiKey = "abc123" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        Assert.Contains("key=abc123", handler.Requests.Single().RequestUri!.Query);
    }

    [Fact]
    public async Task Rejected_key_reports_the_api_message()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, WeatherApiResponses.InvalidKey));

        var result = await CreateSut(handler).ValidateAsync(new WeatherApiConnectionSettings { ApiKey = "wrong" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Equal("API key is invalid.", result.Message);
    }

    private static WeatherApiConnectionType CreateSut(HttpMessageHandler handler)
        => new(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new StubHttpClientFactory(handler));

    /// <summary>ValidateAsync never resolves models; this only satisfies the constructor.</summary>
    private sealed class UnusedModelResolver : IEditableModelResolver
    {
        object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
        TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    }
}
