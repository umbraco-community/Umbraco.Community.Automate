using System.Net;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.Settings;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class DevToArticlePublisherTests
{
    private const string Canonical = "https://owain.codes/blog/my-post/";

    private const string CreatedArticle = """
        {"id":101,"title":"My Post","url":"https://dev.to/owain/my-post-1a2b","slug":"my-post-1a2b","published":false,"canonical_url":"https://owain.codes/blog/my-post/"}
        """;

    private static DevToArticleDraft Draft(bool publish = false, long? existingArticleId = null) => new()
    {
        Title = "My Post",
        BodyMarkdown = "# Hello\n\nWorld",
        CanonicalUrl = Canonical,
        Tags = ["umbraco", "net"],
        Publish = publish,
        ExistingArticleId = existingArticleId,
    };

    private static Task<(string Outcome, DevToArticleOutput Output)> Publish(
        FakeDevToApi api, DevToArticleDraft draft, DevToConnectionSettings? connection = null)
        => new DevToArticlePublisher(new DevToClient(api.CreateFactory()))
            .PublishAsync(connection ?? new DevToConnectionSettings { ApiKey = "secret-key" }, draft, CancellationToken.None);

    [Fact]
    public async Task Creates_a_draft_when_no_article_has_the_canonical_url()
    {
        var api = new FakeDevToApi()
            .RespondWithArticles(new { id = 7, canonical_url = "https://owain.codes/blog/something-else" })
            .Respond(CreatedArticle, HttpStatusCode.Created);

        var (outcome, output) = await Publish(api, Draft());

        outcome.ShouldBe(DevToArticlePublisher.OutcomeCreated);

        api.Requests.Count.ShouldBe(2);
        api.Requests[0].Method.ShouldBe(HttpMethod.Get);
        api.Requests[0].Uri.ToString().ShouldBe("https://dev.to/api/articles/me/all?page=1&per_page=1000");

        var create = api.Requests[1];
        create.Method.ShouldBe(HttpMethod.Post);
        create.Uri.ToString().ShouldBe("https://dev.to/api/articles");
        create.Headers["api-key"].ShouldBe("secret-key");
        create.Headers["Accept"].ShouldBe("application/vnd.forem.api-v1+json");
        create.Headers["User-Agent"].ShouldStartWith("Umbraco.Community.Automate.DevTo/");
        create.ContentType.ShouldBe("application/json");
        create.ContentLength.ShouldBe(System.Text.Encoding.UTF8.GetByteCount(create.Body!), "sent with a Content-Length, not chunked");

        var article = create.Article;
        article.GetProperty("title").GetString().ShouldBe("My Post");
        article.GetProperty("body_markdown").GetString().ShouldBe("# Hello\n\nWorld");
        article.GetProperty("published").GetBoolean().ShouldBeFalse();
        article.GetProperty("canonical_url").GetString().ShouldBe(Canonical);
        article.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ShouldBe(["umbraco", "net"]);
        article.TryGetProperty("series", out _).ShouldBeFalse("blank optional fields are omitted");

        output.ArticleId.ShouldBe(101);
        output.Url.ShouldBe("https://dev.to/owain/my-post-1a2b");
        output.Published.ShouldBeFalse();
    }

    [Fact]
    public async Task Updates_the_article_with_a_matching_canonical_url_without_unpublishing_it()
    {
        var api = new FakeDevToApi()
            .RespondWithArticles(new { id = 55, canonical_url = "http://OWAIN.codes/blog/my-post" })
            .Respond("""{"id":55,"url":"https://dev.to/owain/my-post","published":true}""");

        var (outcome, output) = await Publish(api, Draft());

        outcome.ShouldBe(DevToArticlePublisher.OutcomeUpdated);
        api.Requests[1].Method.ShouldBe(HttpMethod.Put);
        api.Requests[1].Uri.ToString().ShouldBe("https://dev.to/api/articles/55");
        api.Requests[1].Article.TryGetProperty("published", out _).ShouldBeFalse();
        output.ArticleId.ShouldBe(55);
        output.Published.ShouldBeTrue();
    }

    [Fact]
    public async Task Publishing_immediately_publishes()
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond(CreatedArticle);

        await Publish(api, Draft(publish: true));

        api.Requests[1].Article.GetProperty("published").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Existing_article_id_skips_the_canonical_url_lookup()
    {
        var api = new FakeDevToApi().Respond("""{"id":999,"published":false}""");

        var (outcome, _) = await Publish(api, Draft(existingArticleId: 999));

        outcome.ShouldBe(DevToArticlePublisher.OutcomeUpdated);
        api.Requests.ShouldHaveSingleItem().Uri.ToString().ShouldBe("https://dev.to/api/articles/999");
    }

    [Fact]
    public async Task Pages_through_articles_until_a_short_page()
    {
        var fullPage = Enumerable.Range(1, 1000)
            .Select(i => (object)new { id = i, canonical_url = $"https://owain.codes/blog/{i}" })
            .ToArray();

        var api = new FakeDevToApi()
            .RespondWithArticles(fullPage)
            .RespondWithArticles(new { id = 5000, canonical_url = Canonical })
            .Respond("""{"id":5000,"published":true}""");

        var (outcome, _) = await Publish(api, Draft());

        outcome.ShouldBe(DevToArticlePublisher.OutcomeUpdated);
        api.Requests[1].Uri.Query.ShouldContain("page=2");
        api.Requests[2].Uri.ToString().ShouldEndWith("/api/articles/5000");
    }

    [Fact]
    public async Task Uses_the_configured_forem_instance()
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond(CreatedArticle);

        await Publish(api, Draft(), new DevToConnectionSettings { ApiKey = "k", InstanceUrl = "https://community.example.org/" });

        api.Requests[0].Uri.Host.ShouldBe("community.example.org");
    }

    [Fact]
    public async Task Api_key_is_sent_without_surrounding_whitespace()
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond(CreatedArticle);

        await Publish(api, Draft(), new DevToConnectionSettings { ApiKey = "  pasted-key\n" });

        api.Requests[0].Headers["api-key"].ShouldBe("pasted-key");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.UnprocessableEntity, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Api_errors_are_classified_so_automate_can_decide_whether_to_retry(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond("""{"error":"Something went wrong","status":0}""", status);

        var ex = await Should.ThrowAsync<DevToApiException>(() => Publish(api, Draft()));

        ex.Category.ShouldBe(expected);
        ex.Message.ShouldContain("Something went wrong");
    }
}
