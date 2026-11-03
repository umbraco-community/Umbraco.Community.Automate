using System.Net;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Pushover.Api;

/// <summary>A failed call to Pushover, carrying the category actions pass to ActionResult.Failed.</summary>
public sealed class PushoverApiException(string message, StepRunErrorCategory category, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public StepRunErrorCategory Category { get; } = category;

    /// <summary>The HTTP status code, when Pushover responded at all.</summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
