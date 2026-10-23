using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Connections;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.Content;
using Umbraco.Community.Automate.DevTo.Controllers;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;
using static Umbraco.Community.Automate.DevTo.Tests.Helpers.PublishedContentMocks;

namespace Umbraco.Community.Automate.DevTo.Tests.Controllers;

public class DevToControllerTests
{
    private readonly Mock<IAuthorizationService> _authorization = new();
    private readonly Mock<IPublishedContentCache> _cache = new();
    private readonly Mock<IPublishedUrlProvider> _urlProvider = new();
    private readonly DevToArticleLinks _links = new(new InMemoryKeyValueService());

    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Umbraco:Community:Automate:DevTo:Variables:SiteUrl"] = "https://owain.codes",
            ["Umbraco:Community:Automate:DevTo:Secrets:SiteUrl"] = "https://secret.example",
        })
        .Build();

    public DevToControllerTests()
    {
        Authorize(true);

        _urlProvider
            .Setup(p => p.GetUrl(It.IsAny<IPublishedContent>(), UrlMode.Absolute, It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns("https://localhost:44306/blog/my-blog-post/");
        _urlProvider
            .Setup(p => p.GetUrl(It.IsAny<IPublishedContent>(), UrlMode.Relative, It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns("/blog/my-blog-post/");

        var content = Document(TextArea("body", "Read [this](/blog/other/)."));
        _cache.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync(content.Object);
    }

    private DevToController Controller() => new(
        _authorization.Object,
        _cache.Object,
        new DevToContentRenderer(_urlProvider.Object, new ContentMarkdownConverter(_urlProvider.Object, [])),
        _configuration,
        _links,
        new DevToArticleChecker(_links, Mock.Of<IConnectionService>(), new DevToClient(new FakeDevToApi().CreateFactory())))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
    };

    // onlyPermission: grant just that permission, e.g. browse but not update.
    private void Authorize(bool succeeded, string? onlyPermission = null)
        => _authorization
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
            .ReturnsAsync((ClaimsPrincipal _, object? resource, string _) =>
                succeeded && (onlyPermission is null || ((ContentPermissionResource)resource!).PermissionsToCheck.Contains(onlyPermission))
                    ? AuthorizationResult.Success()
                    : AuthorizationResult.Failed());

    private Task<IActionResult> Preview(string? siteUrl = null, string bodyProperties = "body")
        => Controller().Preview(new DevToPreviewRequest(Guid.NewGuid(), bodyProperties, null, siteUrl, null));

    private static DevToPreviewResponse Ok(IActionResult result)
        => (DevToPreviewResponse)result.ShouldBeOfType<OkObjectResult>().Value!;

    private static int? StatusCode(IActionResult result) => result.ShouldBeAssignableTo<IStatusCodeActionResult>()!.StatusCode;

    private Guid PostedContent()
    {
        var content = Guid.NewGuid();
        _links.Save(content, new DevToArticleLink(null, 1, "https://dev.to/owain/post", true, new DateTime(2026, 9, 28, 9, 30, 0, DateTimeKind.Utc)));
        return content;
    }

    [Fact]
    public async Task Preview_returns_the_markdown_and_canonical_url_the_action_would_post()
    {
        var response = Ok(await Preview());

        response.Markdown.ShouldBe("Read [this](https://localhost:44306/blog/other/).");
        response.CanonicalUrl.ShouldBe("https://localhost:44306/blog/my-blog-post/");
        response.Notes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Preview_resolves_automate_variables()
        => Ok(await Preview("$Umbraco:Community:Automate:DevTo:Variables:SiteUrl")).CanonicalUrl.ShouldBe("https://owain.codes/blog/my-blog-post/");

    [Theory]
    [InlineData("$Umbraco:Community:Automate:DevTo:Secrets:SiteUrl")]
    [InlineData("$ConnectionStrings:umbracoDbDSN")]
    [InlineData("${ steps.getContent.properties.siteUrl }")]
    public async Task Preview_never_resolves_secrets_other_configuration_or_bindings(string siteUrl)
    {
        var response = Ok(await Preview(siteUrl));

        response.CanonicalUrl.ShouldBe("https://localhost:44306/blog/my-blog-post/");
        response.Notes.ShouldHaveSingleItem().ShouldContain("Site URL");
    }

    [Fact]
    public async Task Preview_of_unknown_aliases_is_the_error_the_action_would_fail_with()
    {
        var result = (ObjectResult)await Preview(bodyProperties: "body, bodyText");

        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var problem = result.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail!.ShouldContain("'bodyText'");
        problem.Type.ShouldNotBeNull(); // Without one the backoffice shows a generic error instead.
    }

    [Fact]
    public async Task Preview_of_unpublished_content_is_explained()
    {
        _cache.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync((IPublishedContent?)null);

        var result = (ObjectResult)await Preview();

        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        result.Value.ShouldBeOfType<ProblemDetails>().Title.ShouldBe("This content isn't published");
    }

    [Fact]
    public async Task Content_the_user_cannot_browse_is_forbidden()
    {
        Authorize(false);
        var content = PostedContent();

        StatusCode(await Preview()).ShouldBe(StatusCodes.Status403Forbidden);
        StatusCode(await Controller().GetArticleLinks(content)).ShouldBe(StatusCodes.Status403Forbidden);
        StatusCode(await Controller().CheckArticleLinks(content, CancellationToken.None)).ShouldBe(StatusCodes.Status403Forbidden);
        _cache.Verify(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>()), Times.Never);
    }

    [Fact]
    public async Task Returns_the_content_items_article_links()
    {
        var content = PostedContent();

        var result = await Controller().GetArticleLinks(content);

        result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeAssignableTo<IReadOnlyList<DevToArticleLink>>()!.ShouldBe(_links.Get(content));
    }

    [Fact]
    public async Task Removing_an_article_link_needs_update_permission()
    {
        var content = PostedContent();

        Authorize(true, onlyPermission: ActionBrowse.ActionLetter);
        StatusCode(await Controller().RemoveArticleLink(content, null)).ShouldBe(StatusCodes.Status403Forbidden);
        _links.Get(content).ShouldHaveSingleItem();

        Authorize(true, onlyPermission: ActionUpdate.ActionLetter);
        (await Controller().RemoveArticleLink(content, null)).ShouldBeOfType<OkObjectResult>();
        _links.Get(content).ShouldBeEmpty();
    }
}
