using System.Net;
using Umbraco.Community.Automate.Skoda.Client.Models;

namespace Umbraco.Community.Automate.Skoda.Client;

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