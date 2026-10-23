using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Security;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Content;
using Umbraco.Community.Automate.DevTo.Settings;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.Automate.DevTo.Actions;

[Action("devto.publishContent", "Publish Content to DEV",
    ConnectionTypeAlias = DevToConnectionType.ConnectionTypeAlias,
    Description = "Cross-posts a content item to DEV, converting Markdown, Rich Text, Block List and Block Grid content to Markdown.",
    Icon = "icon-automate-devto",
    Group = "Social Networks",
    RequiredSections = [UmbracoConstants.Applications.Content],
    RequiredPermissions = [ActionBrowse.ActionLetter])]
public sealed class PublishContentAction : ActionBase<PublishContentSettings, DevToArticleOutput>
{
    /// <summary>The item left the published cache between the trigger firing and this step running.</summary>
    public const string OutcomeNotFound = "notFound";

    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IAutomationActionAuthorizer _authorizer;
    private readonly DevToContentRenderer _renderer;
    private readonly DevToArticlePublisher _publisher;
    private readonly DevToArticleLinks _links;
    private readonly ILogger<PublishContentAction> _logger;

    public PublishContentAction(
        ActionInfrastructure infrastructure,
        IPublishedContentCache publishedContentCache,
        IUmbracoContextFactory umbracoContextFactory,
        IAutomationActionAuthorizer authorizer,
        DevToContentRenderer renderer,
        DevToArticlePublisher publisher,
        DevToArticleLinks links,
        ILogger<PublishContentAction> logger)
        : base(infrastructure)
    {
        _publishedContentCache = publishedContentCache;
        _umbracoContextFactory = umbracoContextFactory;
        _authorizer = authorizer;
        _renderer = renderer;
        _publisher = publisher;
        _links = links;
        _logger = logger;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var (connection, connectionFailure) = GetConnection(context);
        if (connectionFailure is not null)
            return connectionFailure;

        var settings = context.GetSettings<PublishContentSettings>();

        if (string.IsNullOrWhiteSpace(settings.ContentKey) || !Guid.TryParse(settings.ContentKey, out var contentKey))
            return Invalid($"Invalid or missing content key: '{settings.ContentKey}'.");

        var (articleId, idFailure) = ParseArticleId(settings.ExistingArticleId);
        if (idFailure is not null)
            return idFailure;

        // Section access is checked by middleware; this applies the run identity's start
        // nodes and granular permissions to this specific item.
        if (await _authorizer.AuthorizeContentOrFailAsync(contentKey, RequiredPermissions, cancellationToken) is { } denied)
            return denied;

        // Required when running from the outbox dispatcher, which has no HTTP request scope.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        var content = await _publishedContentCache.GetByIdAsync(contentKey);
        if (content is null)
        {
            _logger.LogDebug(
                "Automation {AutomationId} / Run {RunId}: Content {ContentKey} not found in published cache.",
                context.AutomationId, context.RunId, contentKey);

            return SuccessWithOutcome(OutcomeNotFound, new DevToArticleOutput());
        }

        var (rendered, renderError) = _renderer.Render(content, new DevToRenderOptions(BodyProperties.Parse(settings.BodyProperties).Aliases, settings.Culture, settings.SiteUrl, settings.CanonicalUrl));
        if (rendered is null)
            return renderError!.Category == StepRunErrorCategory.Validation
                ? Invalid(renderError.Message)
                : ActionResult.Failed(new InvalidOperationException(renderError.Message), renderError.Category);

        var title = BoundValue.PlainText(settings.Title) ?? PublishedContentCompat.GetName(content, rendered.Culture);

        if (string.IsNullOrWhiteSpace(title))
            return Invalid("The article title is empty.");

        var draft = new DevToArticleDraft
        {
            Title = title,
            BodyMarkdown = rendered.Body,
            Tags = DevToTags.Normalise(DevToTags.Parse(settings.Tags)),
            CanonicalUrl = rendered.CanonicalUrl,
            Description = BoundValue.PlainText(settings.Description),
            CoverImageUrl = UrlResolver.Resolve(BoundValue.Url(settings.CoverImage), rendered.SiteBaseUri),
            Series = settings.Series,
            Publish = settings.PublishImmediately,
            ExistingArticleId = articleId,
        };

        try
        {
            var (outcome, output) = await _publisher.PublishAsync(connection!, draft, cancellationToken);

            _logger.LogInformation(
                "Automation {AutomationId} / Run {RunId}: Content {ContentKey} posted to DEV as article {ArticleId} ({Outcome}).",
                context.AutomationId, context.RunId, contentKey, output.ArticleId, outcome);

            RememberLink(contentKey, rendered.Culture, context.Connection!.Id, output);

            return SuccessWithOutcome(outcome, output);
        }
        catch (DevToApiException ex)
        {
            return ActionResult.Failed(ex, ex.Category);
        }
    }

    // The article is on DEV either way; failing here would only retry a post that already worked.
    private void RememberLink(Guid contentKey, string? culture, Guid connectionId, DevToArticleOutput output)
    {
        try
        {
            _links.Save(contentKey, new DevToArticleLink(culture, output.ArticleId, output.Url, output.Published, DateTime.UtcNow, connectionId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Content {ContentKey} was posted to DEV as article {ArticleId}, but the link to it couldn't be saved.", contentKey, output.ArticleId);
        }
    }

    private static (DevToConnectionSettings? Settings, ActionResult? Failure) GetConnection(ActionContext context)
    {
        var settings = context.Connection?.GetSettings<DevToConnectionSettings>();

        if (settings is null)
            return (null, ActionResult.Failed(
                new InvalidOperationException($"No {DevToConnectionType.ConnectionTypeAlias} connection is configured for this step."),
                StepRunErrorCategory.ConfigurationError));

        if (DevToConnectionSettingsValidator.Validate(settings) is { } error)
            return (null, ActionResult.Failed(new InvalidOperationException(error), StepRunErrorCategory.ConfigurationError));

        return (settings, null);
    }

    private static (long? Id, ActionResult? Failure) ParseArticleId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (null, null);

        return long.TryParse(value.Trim(), out var id) && id > 0
            ? (id, null)
            : (null, Invalid($"'{value}' is not a valid DEV article ID."));
    }

    private static ActionResult Invalid(string message)
        => ActionResult.Failed(new ArgumentException(message), StepRunErrorCategory.Validation);
}
