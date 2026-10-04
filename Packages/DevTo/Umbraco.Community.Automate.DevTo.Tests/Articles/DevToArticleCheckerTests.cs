using System.Net;
using System.Reflection;
using Moq;
using Shouldly;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;
using Umbraco.Community.Automate.DevTo.Api;
using Umbraco.Community.Automate.DevTo.Connections;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class DevToArticleCheckerTests
{
    private static readonly Guid ConnectionId = Guid.NewGuid();
    private static readonly DateTime Posted = new(2026, 9, 28, 9, 30, 0, DateTimeKind.Utc);

    private readonly Guid _content = Guid.NewGuid();
    private readonly DevToArticleLinks _links = new(new InMemoryKeyValueService());
    private readonly Mock<IConnectionService> _connections = new();
    private readonly FakeDevToApi _api = new();

    public DevToArticleCheckerTests()
    {
        _connections
            .Setup(c => c.GetConfiguredConnectionAsync(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Configured(ConnectionId));
    }

    // ConfiguredConnection's constructor is internal to Automate.
    private static ConfiguredConnection Configured(Guid id)
        => (ConfiguredConnection)Activator.CreateInstance(
            typeof(ConfiguredConnection), BindingFlags.Instance | BindingFlags.NonPublic, null,
            [Connection(id, "community.devto"), Mock.Of<IConnectionType>(), new DevToConnectionSettings { ApiKey = "secret-key" }], null)!;

    private static Connection Connection(Guid id, string type) => new() { Id = id, Alias = type + id.ToString("N")[..6], Name = type, Type = type };

    private DevToArticleChecker Checker() => new(_links, _connections.Object, new DevToClient(_api.CreateFactory()));

    private void Posts(Guid? connectionId = null, bool published = false)
        => _links.Save(_content, new DevToArticleLink(null, 7, "https://dev.to/owain/post-temp-slug-1", published, Posted, connectionId ?? ConnectionId));

    [Fact]
    public async Task A_draft_that_is_still_a_draft_is_marked_checked()
    {
        Posts();
        _api.RespondWithArticles(new { id = 7, url = "https://dev.to/owain/post-temp-slug-1", published = false });

        var result = await Checker().CheckAsync(_content, CancellationToken.None);

        var link = result.Links.ShouldHaveSingleItem();
        link.Published.ShouldBeFalse();
        link.Deleted.ShouldBeFalse();
        link.CheckedUtc.ShouldNotBeNull();
        result.Problems.ShouldBeEmpty();
        _api.Requests.ShouldHaveSingleItem().Headers["api-key"].ShouldBe("secret-key");
    }

    [Fact]
    public async Task Picks_up_an_article_published_on_dev_and_its_new_url()
    {
        Posts();
        _api.RespondWithArticles(new { id = 7, url = "https://dev.to/owain/post-3k2p", published = true });

        var link = (await Checker().CheckAsync(_content, CancellationToken.None)).Links.ShouldHaveSingleItem();

        link.Published.ShouldBeTrue();
        link.Url.ShouldBe("https://dev.to/owain/post-3k2p");
    }

    [Fact]
    public async Task An_article_no_longer_on_dev_is_marked_deleted()
    {
        Posts(published: true);
        _api.RespondWithArticles(new { id = 8, url = "https://dev.to/owain/other", published = true });

        var link = (await Checker().CheckAsync(_content, CancellationToken.None)).Links.ShouldHaveSingleItem();

        link.Deleted.ShouldBeTrue();
        link.Url.ShouldBe("https://dev.to/owain/post-temp-slug-1");
    }

    [Fact]
    public async Task A_dev_error_is_reported_and_leaves_the_link_as_it_was()
    {
        Posts();
        _api.Respond("""{"error":"unauthorized","status":401}""", HttpStatusCode.Unauthorized);

        var result = await Checker().CheckAsync(_content, CancellationToken.None);

        result.Problems.ShouldHaveSingleItem().ShouldContain("unauthorized");
        result.Links.ShouldHaveSingleItem().CheckedUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Links_saved_without_a_connection_use_the_only_dev_connection()
    {
        _links.Save(_content, new DevToArticleLink(null, 7, "https://dev.to/owain/post", true, Posted));
        _connections.Setup(c => c.GetAllConnectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Connection(ConnectionId, "community.devto"), Connection(Guid.NewGuid(), "community.mastodon")]);
        _api.RespondWithArticles(new { id = 7, url = "https://dev.to/owain/post", published = true });

        var result = await Checker().CheckAsync(_content, CancellationToken.None);

        result.Problems.ShouldBeEmpty();
        result.Links.ShouldHaveSingleItem().CheckedUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Links_saved_without_a_connection_are_not_guessed_between_several()
    {
        _links.Save(_content, new DevToArticleLink(null, 7, "https://dev.to/owain/post", true, Posted));
        _connections.Setup(c => c.GetAllConnectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Connection(ConnectionId, "community.devto"), Connection(Guid.NewGuid(), "community.devto")]);

        var result = await Checker().CheckAsync(_content, CancellationToken.None);

        result.Problems.ShouldHaveSingleItem().ShouldContain("more than one DEV connection");
        _api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_deleted_connection_is_reported()
    {
        Posts(connectionId: Guid.NewGuid());

        var result = await Checker().CheckAsync(_content, CancellationToken.None);

        result.Problems.ShouldHaveSingleItem().ShouldContain("no longer exists");
        _api.Requests.ShouldBeEmpty();
    }
}
