using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.Provider1.Actions;
using Xunit;

namespace Umbraco.Community.Automate.Provider1.Tests.Actions;

public class MyAction1ActionTests
{
    [Fact]
    public async Task Execute_WithMessage_ReturnsProcessedOutput()
    {
        var result = await ActionTestHarness
            .For<MyAction1Action>()
            .WithSettings(new MyAction1Settings { Message = "Hello" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = (MyAction1Output)result.OutputData!;
        output.ProcessedMessage.ShouldBe("Processed: Hello");
    }

    [Fact]
    public async Task Execute_WithEmptyMessage_ReturnsFailed()
    {
        var result = await ActionTestHarness
            .For<MyAction1Action>()
            .WithSettings(new MyAction1Settings { Message = "" })
            .ExecuteAsync();

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }
}
