using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.TestConnection.Actions;
using Umbraco.Community.Automate.TestConnection.Tests.Fakes;
using Xunit;

namespace Umbraco.Community.Automate.TestConnection.Tests.Actions;

public class TestConnectionActionTests
{
    private static readonly Connection Pushover = new() { Id = Guid.NewGuid(), Alias = "pushover", Name = "Pushover", Type = "community.pushover" };

    [Fact]
    public async Task A_passing_check_is_connected_with_its_message()
    {
        var service = new FakeConnectionService(Pushover, ConnectionValidationResult.Success("Connected as @paul."));

        var output = await Run(service, "pushover");

        Assert.True(output.Connected);
        Assert.Equal("Connected as @paul.", output.Message);
        Assert.Equal(Pushover.Id, Assert.Single(service.Tested));
    }

    [Fact]
    public async Task A_warning_still_counts_as_connected()
    {
        var output = await Run(new FakeConnectionService(Pushover, ConnectionValidationResult.Warning("Live check skipped.", [])), "pushover");

        Assert.True(output.Connected);
        Assert.Equal("Live check skipped.", output.Message);
    }

    [Fact]
    public async Task A_failing_check_succeeds_as_a_step_but_is_not_connected()
    {
        var output = await Run(new FakeConnectionService(Pushover, ConnectionValidationResult.Failure("Token is invalid.", [])), "pushover");

        Assert.False(output.Connected);
        Assert.Equal("Token is invalid.", output.Message);
    }

    [Fact]
    public async Task An_unknown_alias_is_not_connected_and_tests_nothing()
    {
        var service = new FakeConnectionService(Pushover, ConnectionValidationResult.Success("unused"));

        var output = await Run(service, "mastodon");

        Assert.False(output.Connected);
        Assert.Contains("mastodon", output.Message);
        Assert.Empty(service.Tested);
    }

    [Fact]
    public async Task A_connection_that_cannot_be_tested_is_not_connected()
    {
        var output = await Run(new FakeConnectionService(Pushover, null), "pushover");

        Assert.False(output.Connected);
    }

    private static async Task<TestConnectionOutput> Run(FakeConnectionService service, string alias)
    {
        var result = await ActionTestHarness.For<TestConnectionAction>()
            .WithService<IConnectionService>(service)
            .WithSettings(new TestConnectionSettings { ConnectionAlias = alias })
            .ExecuteAsync();

        Assert.Equal(ActionResultStatus.Success, result.Status);
        return Assert.IsType<TestConnectionOutput>(result.OutputData);
    }
}
