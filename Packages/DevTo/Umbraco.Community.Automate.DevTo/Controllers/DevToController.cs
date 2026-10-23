using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Content;
using Umbraco.Community.Automate.DevTo.Settings;

namespace Umbraco.Community.Automate.DevTo.Controllers;

public sealed record DevToPreviewRequest(Guid ContentKey, string? BodyProperties, string? Culture, string? SiteUrl, string? CanonicalUrl);

public sealed record DevToPreviewResponse(string? Name, string? Culture, string CanonicalUrl, string Markdown, IReadOnlyList<string> Notes);

/// <summary>
/// The backoffice's calls (see wwwroot/api.js): the Markdown preview, and the Info tab's links to
/// the DEV articles a content item was posted as.
/// </summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("devto")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public sealed class DevToController : ManagementApiControllerBase
{
    // Only variables: a preview must never echo secrets or arbitrary configuration back to the browser.
    private const string VariablesPrefix = DevToConfiguration.VariablesPath + ":";

    private readonly IAuthorizationService _authorizationService;
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly DevToContentRenderer _renderer;
    private readonly IConfiguration _configuration;
    private readonly DevToArticleLinks _links;
    private readonly DevToArticleChecker _checker;

    public DevToController(
        IAuthorizationService authorizationService,
        IPublishedContentCache publishedContentCache,
        DevToContentRenderer renderer,
        IConfiguration configuration,
        DevToArticleLinks links,
        DevToArticleChecker checker)
    {
        _authorizationService = authorizationService;
        _publishedContentCache = publishedContentCache;
        _renderer = renderer;
        _configuration = configuration;
        _links = links;
        _checker = checker;
    }

    /// <summary>Converts a content item the way Publish Content to DEV would, without calling DEV.</summary>
    [HttpPost("preview")]
    [ProducesResponseType<DevToPreviewResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(DevToPreviewRequest request)
    {
        if (!await CanAsync(ActionBrowse.ActionLetter, request.ContentKey))
            return Forbidden();

        var content = await _publishedContentCache.GetByIdAsync(request.ContentKey);
        if (content is null)
            return Failure("This content isn't published", "The action only posts published content. Publish it, then preview again.");

        var notes = new List<string>();
        var options = new DevToRenderOptions(
            BodyProperties.Parse(request.BodyProperties).Aliases,
            Literal(request.Culture, "Culture", notes),
            Literal(request.SiteUrl, "Site URL", notes),
            Literal(request.CanonicalUrl, "Canonical URL", notes));

        var (rendered, error) = _renderer.Render(content, options);
        if (rendered is null)
            return Failure("The action would fail", error!.Message);

        return Ok(new DevToPreviewResponse(
            PublishedContentCompat.GetName(content, rendered.Culture), rendered.Culture, rendered.CanonicalUrl, rendered.Body, notes));
    }

    [HttpGet("article-links/{contentKey:guid}")]
    [ProducesResponseType<IReadOnlyList<DevToArticleLink>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArticleLinks(Guid contentKey)
        => await CanAsync(ActionBrowse.ActionLetter, contentKey) ? Ok(_links.Get(contentKey)) : Forbidden();

    /// <summary>Asks DEV what became of the linked articles. The only call that reaches DEV, and only on request.</summary>
    [HttpPost("article-links/{contentKey:guid}/check")]
    [ProducesResponseType<DevToArticleCheckResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckArticleLinks(Guid contentKey, CancellationToken cancellationToken)
        => await CanAsync(ActionBrowse.ActionLetter, contentKey) ? Ok(await _checker.CheckAsync(contentKey, cancellationToken)) : Forbidden();

    [HttpDelete("article-links/{contentKey:guid}")]
    [ProducesResponseType<IReadOnlyList<DevToArticleLink>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveArticleLink(Guid contentKey, string? culture)
    {
        if (!await CanAsync(ActionUpdate.ActionLetter, contentKey))
            return Forbidden();

        _links.Remove(contentKey, culture);
        return Ok(_links.Get(contentKey));
    }

    private async Task<bool> CanAsync(string permission, Guid contentKey)
        => (await _authorizationService.AuthorizeAsync(
            User, ContentPermissionResource.WithKeys(permission, contentKey), AuthorizationPolicies.ContentPermissionByResource)).Succeeded;

    // Bindings only have values during a run, so the preview falls back to what a blank setting does.
    private string? Literal(string? value, string label, List<string> notes)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        if (value.Contains("${"))
        {
            notes.Add($"{label} uses a binding, which only resolves when the automation runs, so the preview left it blank.");
            return null;
        }

        if (!value.StartsWith('$'))
            return value;

        var key = value[1..];
        if (key.StartsWith(VariablesPrefix, StringComparison.OrdinalIgnoreCase) && _configuration[key] is { Length: > 0 } resolved)
            return resolved;

        notes.Add($"{label} references configuration the preview can't read ({value}), so the preview left it blank.");
        return null;
    }

    // A 400 with a Type: the backoffice replaces the body of a 404, or of problem details without a Type, with a generic message.
    private BadRequestObjectResult Failure(string title, string detail)
        => BadRequest(new ProblemDetails { Title = title, Detail = detail, Status = StatusCodes.Status400BadRequest, Type = "Error" });
}
