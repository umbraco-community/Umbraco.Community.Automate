using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Security;
using Umbraco.Automate.Testing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.Automate.DevTo.Actions;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.Content;
using Umbraco.Community.Automate.DevTo.Settings;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;
using static Umbraco.Community.Automate.DevTo.Tests.Helpers.PublishedContentMocks;

namespace Umbraco.Community.Automate.DevTo.Tests.Actions;

public class PublishContentActionTests
{
    private const string CreatedArticle = """{"id":1,"url":"https://dev.to/owain/my-blog-post","published":false}""";

    private readonly Mock<IPublishedContentCache> _cache = new();
    private readonly Mock<IPublishedUrlProvider> _urlProvider = new();
    private readonly Mock<IAutomationActionAuthorizer> _authorizer = new();
    private readonly FakeDevToApi _api = new();
    private IKeyValueService _keyValues = new InMemoryKeyValueService();

    public PublishContentActionTests()
    {
        _authorizer
            .Setup(a => a.AuthorizeContentAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlySet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutomationAuthorizationResult.Success);

        _urlProvider
            .Setup(p => p.GetUrl(It.IsAny<IPublishedContent>(), UrlMode.Absolute, It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns("https://owain.codes/blog/my-blog-post/");
        _urlProvider
            .Setup(p => p.GetUrl(It.IsAny<IPublishedContent>(), UrlMode.Relative, It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns("/blog/my-blog-post/");
        _urlProvider
            .Setup(p => p.GetMediaUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Uri?>()))
            .Returns<IPublishedContent, UrlMode, string?, string, Uri?>((media, _, _, _, _) => $"/media/{media.Name}.jpg");
    }

    private Mock<IPublishedContent> BlogPost()
    {
        var blocks = new BlockListModel([
            new BlockListItem(Guid.NewGuid(), Element(TextBox("heading", "Why")), null, null),
            new BlockListItem(Guid.NewGuid(), Element(Property("text", "Umbraco.RichText",
                new Umbraco.Cms.Core.Strings.HtmlEncodedString("<p>Read <a href=\"/blog/other/\">this</a>.</p>"))), null, null),
        ]);

        var content = Document(
            TextBox("pageTitle", "A better title"),
            Property("blocks", "Umbraco.BlockList", blocks),
            Property("tags", "Umbraco.Tags", new[] { "Umbraco", "Automate" }),
            TextArea("summary", "Short summary"),
            Property("hero", "Umbraco.MediaPicker3", Media("hero")));

        _cache.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync(content.Object);
        return content;
    }

    private Task<ActionResult> Execute(PublishContentSettings settings, DevToConnectionSettings? connection = null)
    {
        var contextFactory = new Mock<IUmbracoContextFactory>();
        contextFactory
            .Setup(f => f.EnsureUmbracoContext())
            .Returns(new UmbracoContextReference(Mock.Of<IUmbracoContext>(), false, Mock.Of<IUmbracoContextAccessor>()));

        return ActionTestHarness.For<PublishContentAction>()
            .WithService(_cache.Object)
            .WithService(contextFactory.Object)
            .WithService(_authorizer.Object)
            .WithService(new DevToContentRenderer(_urlProvider.Object, new ContentMarkdownConverter(_urlProvider.Object, [])))
            .WithService(new DevToArticlePublisher(new DevToClient(_api.CreateFactory())))
            .WithService(new DevToArticleLinks(_keyValues))
            .WithService<ILogger<PublishContentAction>>(NullLogger<PublishContentAction>.Instance)
            .WithSettings(settings)
            .WithConnection("devto", connection ?? new DevToConnectionSettings { ApiKey = "secret-key" })
            .ExecuteAsync();
    }

    private static PublishContentSettings Settings(Action<PublishContentSettings>? configure = null)
    {
        var settings = new PublishContentSettings
        {
            ContentKey = Guid.NewGuid().ToString(),
            BodyProperties = "blocks",
            // What the bindings resolve to by the time the action runs: plain text, the JSON a
            // Tags editor binds to, HTML from Rich Text, and a media picker's JSON.
            Title = "A better title",
            Tags = """["Umbraco","Automate"], dotnet, webdev, blog""",
            Description = "<p>Short <strong>summary</strong></p>",
            CoverImage = """[{"key":"2a1c9d0e-0000-0000-0000-000000000000","name":"hero","url":"/media/hero.jpg"}]""",
        };
        configure?.Invoke(settings);
        return settings;
    }

    [Fact]
    public async Task Posts_converted_content_with_canonical_url_tags_and_cover_image()
    {
        BlogPost();
        _api.RespondWithArticles().Respond(CreatedArticle);

        var result = await Execute(Settings());

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeCreated);

        var article = _api.Requests[1].Article;
        article.GetProperty("title").GetString().ShouldBe("A better title");
        article.GetProperty("body_markdown").GetString().ShouldBe("## Why\n\nRead [this](https://owain.codes/blog/other/).");
        article.GetProperty("canonical_url").GetString().ShouldBe("https://owain.codes/blog/my-blog-post/");
        article.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ShouldBe(["umbraco", "automate", "dotnet", "webdev"]);
        article.GetProperty("description").GetString().ShouldBe("Short summary");
        article.GetProperty("main_image").GetString().ShouldBe("https://owain.codes/media/hero.jpg");
        article.GetProperty("published").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Blank_title_falls_back_to_the_content_name()
    {
        BlogPost();
        _api.RespondWithArticles().Respond(CreatedArticle);

        await Execute(Settings(s => s.Title = ""));

        _api.Requests[1].Article.GetProperty("title").GetString().ShouldBe("My Blog Post");
    }

    [Fact]
    public async Task Site_url_setting_overrides_the_host_umbraco_generates()
    {
        BlogPost();
        _api.RespondWithArticles().Respond(CreatedArticle);

        await Execute(Settings(s => s.SiteUrl = "https://www.example.com"));

        var article = _api.Requests[1].Article;
        article.GetProperty("canonical_url").GetString().ShouldBe("https://www.example.com/blog/my-blog-post/");
        article.GetProperty("body_markdown").GetString()!.ShouldContain("(https://www.example.com/blog/other/)");
    }

    [Fact]
    public async Task Fails_with_a_configuration_error_when_no_absolute_url_can_be_produced()
    {
        BlogPost();
        _urlProvider
            .Setup(p => p.GetUrl(It.IsAny<IPublishedContent>(), UrlMode.Absolute, It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns("/blog/my-blog-post/");

        var result = await Execute(Settings());

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
        result.Exception!.Message.ShouldContain("Site URL");
        _api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reads_body_properties_stored_by_the_picker()
    {
        BlogPost();
        _api.RespondWithArticles().Respond(CreatedArticle);

        var result = await Execute(Settings(s => s.BodyProperties = $$"""{"documentType":"{{Guid.NewGuid()}}","aliases":["blocks"]}"""));

        result.Status.ShouldBe(ActionResultStatus.Success);
        _api.Requests[1].Article.GetProperty("body_markdown").GetString().ShouldStartWith("## Why");
    }

    [Fact]
    public async Task Remembers_the_article_for_the_content_items_info_tab()
    {
        BlogPost();
        _api.RespondWithArticles().Respond(CreatedArticle);
        var settings = Settings();

        await Execute(settings);

        var link = new DevToArticleLinks(_keyValues).Get(Guid.Parse(settings.ContentKey)).ShouldHaveSingleItem();
        link.ArticleId.ShouldBe(1);
        link.Url.ShouldBe("https://dev.to/owain/my-blog-post");
        link.Published.ShouldBeFalse();
        link.ConnectionId.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_link_that_cannot_be_saved_does_not_fail_a_post_that_worked()
    {
        BlogPost();
        _api.RespondWithArticles().Respond(CreatedArticle);
        var failing = new Mock<IKeyValueService>();
        failing.Setup(k => k.SetValue(It.IsAny<string>(), It.IsAny<string>())).Throws(new InvalidOperationException("Database is locked"));
        _keyValues = failing.Object;

        var result = await Execute(Settings());

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeCreated);
    }

    [Fact]
    public async Task Unknown_property_aliases_fail_validation_and_name_the_aliases()
    {
        BlogPost();

        var result = await Execute(Settings(s => s.BodyProperties = "blocks, bodyText"));

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("'bodyText'");
    }

    [Fact]
    public async Task Content_missing_from_the_published_cache_produces_notFound()
    {
        _cache.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync((IPublishedContent?)null);

        var result = await Execute(Settings());

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe(PublishContentAction.OutcomeNotFound);
        _api.Requests.ShouldBeEmpty();
        ((InMemoryKeyValueService)_keyValues).Values.ShouldBeEmpty();
    }

    [Fact]
    public async Task Content_the_run_identity_cannot_access_is_rejected()
    {
        BlogPost();
        _authorizer
            .Setup(a => a.AuthorizeContentAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlySet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutomationAuthorizationResult.Fail("Outside start node."));

        var result = await Execute(Settings());

        result.Status.ShouldBe(ActionResultStatus.Failed);
        _api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unresolved_api_key_reference_is_a_configuration_error()
    {
        BlogPost();

        var result = await Execute(Settings(), new DevToConnectionSettings { ApiKey = "$Umbraco:Community:Automate:DevTo:Secrets:Missing" });

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
        _api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Invalid_content_key_fails_validation()
    {
        var result = await Execute(Settings(s => s.ContentKey = "not-a-guid"));

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }
}
