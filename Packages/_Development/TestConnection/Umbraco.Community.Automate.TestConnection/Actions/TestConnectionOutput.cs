namespace Umbraco.Community.Automate.TestConnection.Actions;

public sealed class TestConnectionOutput
{
    /// <summary>Whether the connection's check passed.</summary>
    public bool Connected { get; init; }

    /// <summary>What the connection's check reported, e.g. "Connected as @name" or why it failed.</summary>
    public string Message { get; init; } = string.Empty;
}
