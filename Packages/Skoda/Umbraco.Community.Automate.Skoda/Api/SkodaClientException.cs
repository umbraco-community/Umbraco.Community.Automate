using System.Net;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Api;

internal sealed class SkodaClientException(
    string message,
    HttpStatusCode statusCode,
    ProblemDetail? problem,
    string? response)
    : HttpRequestException(message, null, statusCode)
{
    public ProblemDetail? Problem { get; } = problem;
    public string? Response { get; } = response;
}