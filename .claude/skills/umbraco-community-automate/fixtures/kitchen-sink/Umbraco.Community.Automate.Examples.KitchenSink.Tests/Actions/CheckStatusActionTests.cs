using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Examples.KitchenSink.Actions;
using Umbraco.Community.Automate.Examples.KitchenSink.Api;
using Umbraco.Community.Automate.Examples.KitchenSink.Connections;
using Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Actions;

public class CheckStatusActionTests
{
    [Fact]
    public async Task Success_status_gives_the_available_outcome()
    {
        var result = await Run(HttpStatusCode.OK, 200);

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(CheckStatusAction.OutcomeAvailable, result.Outcome);
        Assert.Equal(200, Assert.IsType<CheckStatusOutput>(result.OutputData).StatusCode);
    }

    [Fact]
    public async Task Not_found_is_an_outcome_not_a_failure()
    {
        var result = await Run(HttpStatusCode.NotFound, 404);

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Equal(CheckStatusAction.OutcomeNotFound, result.Outcome);
    }

    [Fact]
    public async Task Server_error_is_a_retryable_failure()
    {
        var result = await Run(HttpStatusCode.ServiceUnavailable, 503);

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(StepRunErrorCategory.ServiceUnavailable, result.ErrorCategory);
    }

    [Fact]
    public async Task Out_of_range_status_code_is_a_validation_error()
    {
        var handler = new StubHttpMessageHandler();

        var result = await ActionTestHarness.For<CheckStatusAction>()
            .WithService(new KitchenSinkClient(new StubHttpClientFactory(handler)))
            .WithSettings(new CheckStatusSettings { StatusCode = 42 })
            .WithConnection("community.examples.kitchenSink", new KitchenSinkConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();

        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Empty(handler.Requests);
    }

    private static Task<ActionResult> Run(HttpStatusCode answer, int requested)
        => ActionTestHarness.For<CheckStatusAction>()
            .WithService(new KitchenSinkClient(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage(answer)))))
            .WithSettings(new CheckStatusSettings { StatusCode = requested })
            .WithConnection("community.examples.kitchenSink", new KitchenSinkConnectionSettings { ApiKey = "token" })
            .ExecuteAsync();
}
