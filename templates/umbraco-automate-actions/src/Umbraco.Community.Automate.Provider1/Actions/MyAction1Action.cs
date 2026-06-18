using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Provider1.Actions;

/// <summary>
/// A sample action. Customize this to implement your integration logic.
/// </summary>
[Action("myProvider1.myAction1", "MyAction1",
    Description = "A sample action — customize this to do something useful.",
    Group = "Custom",
    Icon = "icon-flash")]
public sealed class MyAction1Action : ActionBase<MyAction1Settings, MyAction1Output>
{
    public MyAction1Action(ActionInfrastructure infrastructure)
        : base(infrastructure) { }

    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<MyAction1Settings>();

        if (string.IsNullOrWhiteSpace(settings.Message))
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException("Message is required."),
                StepRunErrorCategory.Validation));
        }

        return Task.FromResult(Success(new MyAction1Output
        {
            ProcessedMessage = $"Processed: {settings.Message}",
        }));
    }
}
