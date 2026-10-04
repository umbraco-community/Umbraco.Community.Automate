using System.Net;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.Examples.Simple.Configuration;
using Umbraco.Community.Automate.Examples.Simple.Connections;
using Umbraco.Community.Automate.Examples.Simple.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.Simple.Tests.Connections;

public class SimpleConnectionTypeTests
{
    [Fact]
    public void New_connections_start_with_the_configuration_reference()
        => Assert.Equal("$Umbraco:Automate:Secrets:Simple:ApiKey", new SimpleConnectionSettings().ApiKey);

    [Fact]
    public async Task Accepted_key_connects()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var result = await CreateSut(handler).ValidateAsync(new SimpleConnectionSettings { ApiKey = "token" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Success, result.Status);
        var request = handler.Requests.Single();
        Assert.Equal($"{SimpleConfiguration.BaseUrl}bearer", request.RequestUri!.ToString());
        Assert.Equal("Bearer token", request.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Missing_key_fails_without_calling_the_service()
    {
        var handler = new StubHttpMessageHandler();

        var result = await CreateSut(handler).ValidateAsync(new SimpleConnectionSettings { ApiKey = "" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejected_key_fails_with_the_status_code()
    {
        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var result = await CreateSut(handler).ValidateAsync(new SimpleConnectionSettings { ApiKey = "token" }, CancellationToken.None);

        Assert.Equal(ConnectionValidationStatus.Failure, result.Status);
        Assert.Contains("401", result.Message);
    }

    private static SimpleConnectionType CreateSut(HttpMessageHandler handler)
        => new(new ConnectionTypeInfrastructure(new UnusedModelResolver()), new StubHttpClientFactory(handler));

    /// <summary>ValidateAsync never resolves models; this only satisfies the constructor.</summary>
    private sealed class UnusedModelResolver : IEditableModelResolver
    {
        object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
        TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    }
}
