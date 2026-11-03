namespace Umbraco.Community.Automate.Examples.KitchenSink.Actions;

/// <summary>What <see cref="CheckStatusAction"/> gives later steps: <c>${ steps.&lt;alias&gt;.statusCode }</c>.</summary>
public sealed class CheckStatusOutput
{
    public int StatusCode { get; init; }
}
