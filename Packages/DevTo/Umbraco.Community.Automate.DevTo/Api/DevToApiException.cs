using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.DevTo.Api;

/// <summary>
/// A failed call to the Forem API, already classified so actions can hand the category
/// straight to Automate — which retries the transient ones (rate limiting, timeouts,
/// service unavailable) and fails fast on the rest.
/// </summary>
public sealed class DevToApiException : Exception
{
    public DevToApiException(string message, StepRunErrorCategory category, Exception? inner = null)
        : base(message, inner)
    {
        Category = category;
    }

    public StepRunErrorCategory Category { get; }
}
