namespace Umbraco.Community.Automate.Provider1.Actions;

/// <summary>
/// Output produced by the MyAction1 action. These values are available
/// as binding data for subsequent steps via ${steps.stepId.processedMessage}.
/// </summary>
public sealed class MyAction1Output
{
    public string? ProcessedMessage { get; init; }
}
