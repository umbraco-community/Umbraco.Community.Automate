using Shouldly;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class DevToArticleLinksTests
{
    private static readonly DateTime Posted = new(2026, 9, 28, 9, 30, 0, DateTimeKind.Utc);

    private readonly InMemoryKeyValueService _store = new();
    private readonly DevToArticleLinks _links;

    public DevToArticleLinksTests() => _links = new DevToArticleLinks(_store);

    private static DevToArticleLink Link(string? culture = null, long id = 1, bool published = false)
        => new(culture, id, $"https://dev.to/owain/post-{id}", published, Posted);

    [Fact]
    public void Nothing_posted_is_empty() => _links.Get(Guid.NewGuid()).ShouldBeEmpty();

    [Fact]
    public void Round_trips_the_last_post()
    {
        var content = Guid.NewGuid();

        _links.Save(content, Link(id: 1));
        _links.Save(content, Link(id: 1, published: true));

        _links.Get(content).ShouldHaveSingleItem().ShouldBe(Link(id: 1, published: true));
    }

    [Fact]
    public void Keeps_one_link_per_culture_and_only_for_that_content()
    {
        var content = Guid.NewGuid();
        _links.Save(content, Link("en-US", 1));
        _links.Save(content, Link("da-DK", 2));
        _links.Save(Guid.NewGuid(), Link("en-US", 3));

        _links.Get(content).Select(l => l.ArticleId).ShouldBe([2, 1]);
    }

    [Fact]
    public void Skips_values_it_cannot_read()
    {
        var content = Guid.NewGuid();
        _links.Save(content, Link("en-US"));
        _store.Values[_store.Values.Keys.Single() + "x"] = "{not json";

        _links.Get(content).ShouldHaveSingleItem();
    }

    [Fact]
    public void Removed_links_are_gone()
    {
        var content = Guid.NewGuid();
        _links.Save(content, Link("en-US", 1));
        _links.Save(content, Link("da-DK", 2));

        _links.Remove(content, "en-US");

        _links.Get(content).ShouldHaveSingleItem().Culture.ShouldBe("da-DK");
    }

    [Fact]
    public void Reads_links_saved_before_connections_and_checks_were_recorded()
    {
        var content = Guid.NewGuid();
        _links.Save(content, Link());
        var key = _store.Values.Keys.Single();
        _store.Values[key] = """{"culture":null,"articleId":1,"url":"https://dev.to/owain/post-1","published":true,"postedUtc":"2026-09-28T09:30:00Z"}""";

        var link = _links.Get(content).ShouldHaveSingleItem();

        link.ConnectionId.ShouldBeNull();
        link.Deleted.ShouldBeFalse();
        link.CheckedUtc.ShouldBeNull();
    }
}
