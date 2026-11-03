using System.Net;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.Community.Automate.Mastodon.Api;

/// <summary>Maps a failed Mastodon response to the category Automate uses to decide whether to retry.</summary>
public static class MastodonErrors
{
    /// <summary>
    /// Rate limits and outages are temporary, so Automate retries them; a rejected token or an
    /// invalid post (e.g. too long, 422) needs the user to act.
    /// </summary>
    public static StepRunErrorCategory Classify(HttpStatusCode statusCode) => (int)statusCode switch
    {
        401 or 403 => StepRunErrorCategory.Authentication,
        429 => StepRunErrorCategory.RateLimiting,
        >= 400 and < 500 => StepRunErrorCategory.Validation,
        >= 500 => StepRunErrorCategory.ServiceUnavailable,
        _ => StepRunErrorCategory.InvalidResponse,
    };
}
