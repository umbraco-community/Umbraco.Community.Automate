using System.Net;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Api;

/// <summary>
/// A failed call to the service, carrying the <see cref="StepRunErrorCategory"/> that actions
/// pass straight to <c>ActionResult.Failed</c>, so every action reports errors the same way.
/// </summary>
public sealed class KitchenSinkApiException(string message, StepRunErrorCategory category, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public StepRunErrorCategory Category { get; } = category;

    /// <summary>The HTTP status code, when the service responded at all.</summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
