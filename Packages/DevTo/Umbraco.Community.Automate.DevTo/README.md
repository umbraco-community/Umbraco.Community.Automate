# Umbraco.Community.Automate.DevTo

A [DEV Community](https://dev.to) (dev.to) connection and action for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

Cross-post your Umbraco content to DEV automatically when you publish it. Markdown, Rich Text, Block List and Block Grid content is converted to Markdown, relative links and images are made absolute, and the canonical URL points back at your site so search engines treat it as the original.

Works with **Umbraco 17 and 18** (and Umbraco Automate 17 and 18).

## Installation

```bash
dotnet add package Umbraco.Community.Automate.DevTo
```

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

### 1. Generate a DEV API key

On DEV, go to **Settings → Extensions → DEV Community API Keys**, give the key a description and click **Generate API Key**.

### 2. Store the key as a secret

Rather than pasting values into the backoffice, store them under a `DevTo` key in Umbraco Automate's shared **Variables** (non-sensitive) and **Secrets** (sensitive) sections. Umbraco Automate resolves `$` references from its shared `Umbraco:Automate:Variables` and `Umbraco:Automate:Secrets` sections out of the box, so there's nothing to register. `Secrets` can only be referenced from sensitive fields such as **API Key**.

```json
{
  "Umbraco": {
    "Automate": {
      "Variables": {
        "DevTo": {
          "SiteUrl": "https://your-site.com"
        }
      },
      "Secrets": {
        "DevTo": {
          "ApiKey": "your-api-key"
        }
      }
    }
  }
}
```

For production, use environment variables instead:

```
Umbraco__Automate__Secrets__DevTo__ApiKey=your-api-key
Umbraco__Automate__Variables__DevTo__SiteUrl=https://your-site.com
```

Reference these values from the backoffice with a `$` prefix, e.g. `$Umbraco:Automate:Variables:DevTo:SiteUrl`.

`SiteUrl` is only needed if Umbraco can't generate absolute URLs for your content (see [URLs](#urls)).

### 3. Create the connection

1. Go to **Automation → Settings → Connections** and create a new **DEV Community** connection.
2. **API Key** is already filled in with `$Umbraco:Automate:Secrets:DevTo:ApiKey`, a reference to the key you stored in step 2. Leave it, or replace it with the key itself to store it on the connection.
3. Click **Test connection**. You should see "Connected as @yourname".

Posting to a different [Forem](https://forem.com) community? Change **Instance URL** under *Advanced*.

> **Reference not resolving?** Umbraco Automate reports *Configuration key '...' not found* when the key isn't in configuration. The Markdown preview only resolves references under `Umbraco:Automate:Variables:DevTo`, and never secrets.

## Cross-posting blog posts

Create an automation:

1. **Trigger:** *Content Published*, with **Content Types** set to your blog post type.
2. **Action:** *Get Content*, with Content Key `${ trigger.contentKey }`. Only needed if you bind
   tags, a description or a cover image from the post's properties (below).
3. **Action:** *Publish Content to DEV*, with your DEV connection.

| Setting | Description |
|---|---|
| Body Properties | The properties holding the body, in order. Click **Choose from a document type…**, pick your blog post type and tick its body properties (only Markdown, Rich Text, Block List, Block Grid and Textarea properties are listed, including ones from compositions), or type an alias. |
| Title | Defaults to `${ trigger.contentName }`. Bind a property instead, e.g. `${ steps.getContent.properties.pageTitle }`, or compose one: `${ trigger.contentName } \| My Blog`. Blank uses the content name. |
| Tags | Typed (`umbraco, dotnet`), bound from a Tags or picker property (`${ steps.getContent.properties.tags }`), or both: `umbraco, ${ steps.getContent.properties.categories }`. |
| Publish immediately | Off (default): new articles are saved as drafts on DEV. On: they're published. |
| Description | A summary for feeds and link previews, e.g. `${ steps.getContent.properties.metaDescription }`. HTML is stripped. |
| Cover Image | A URL, or a bound media picker, e.g. `${ steps.getContent.properties.mainImage }`. |
| Series | Links articles together as a series on DEV. |
| Content Key | *Advanced.* The item to post. Defaults to `${ trigger.contentKey }`. |
| Culture | *Advanced.* For variant content. Blank uses the default culture. |
| Site URL | *Advanced.* Your public base URL, e.g. `$Umbraco:Automate:Variables:DevTo:SiteUrl`. |
| Canonical URL | *Advanced.* Override the canonical URL. |
| Existing Article ID | *Advanced.* Update this DEV article instead of looking one up. |

Title, Tags, Description and Cover Image take `${ }` bindings,
so they can come from the trigger, a *Get Content* step or any earlier step, with filters such as
`| truncate:100`. Bound pickers and media arrive as JSON; the action reads the names (for tags) and
URLs (for images) out of it.

Body Properties are properties rather than bindings on purpose: converting blocks, media and Markdown
needs the content itself, which a binding (a JSON copy of the values) can't provide. Picked
properties are stored with the document type they came from, so reopening the step shows their
names and flags any alias the type no longer has (after a property is renamed, for example). Typed
aliases are stored as a plain list (e.g. `intro, contentRows`).

Tags are lowercased and stripped to letters and numbers, as DEV requires ("Umbraco CMS" becomes `umbracocms`), and only the first four are used.

### Updates, not duplicates

Before posting, the action looks through your DEV articles (published and drafts) for one with the same canonical URL. If it finds one, it updates it; otherwise it creates a new one. So re-publishing a post in Umbraco updates its copy on DEV, and a retried step never posts twice. No extra property on your document type is needed.

If you change a post's URL, the lookup won't find the old article. Set **Existing Article ID** to point it at the right one.

Leaving **Publish immediately** off never unpublishes. New articles are created as drafts, and an article you've already published on DEV stays published when it's updated.

### Outcomes and outputs

The action produces a `created`, `updated` or `notFound` outcome (`notFound` means the content was unpublished before the step ran), so later steps can branch. For example, announce new posts on Mastodon only on `created`, so edits don't post again.

Its output is available to later steps:

| Output | Example |
|---|---|
| `${ steps.<alias>.url }` | `https://dev.to/you/my-post-1a2b` |
| `${ steps.<alias>.articleId }` | `1234567` |
| `${ steps.<alias>.slug }` | `my-post-1a2b` |
| `${ steps.<alias>.published }` | `false` |
| `${ steps.<alias>.canonicalUrl }` | `https://your-site.com/blog/my-post/` |

### Previewing

Click **Preview…** under Body Properties and pick a published item to see the Markdown and canonical
URL the step would post, or the error it would fail with. DEV isn't called. Bindings only have values
during a run, so the preview uses what a blank setting would for any that use one (Site URL, Culture,
Canonical URL); `$Umbraco:Automate:Variables:DevTo:…` references are resolved, secrets never are.

### Finding the article on DEV

Once a post has gone out, its document's **Info** tab shows a **DEV** box with a link to the article,
whether it was a draft or published when it was last posted, and when. Variant content gets a link per
culture. The link is kept in Umbraco's key-value store, so your document type doesn't need a property
for it. Automate's run history doesn't show step output, so this is the place to look.

The backoffice never calls DEV by itself. Click **Check on DEV** to ask DEV for the article's current
state, using the connection it was posted with: it picks up an article you've since published (and its
new URL), or marks one you've deleted as **Deleted on DEV**, with a button to remove the link. Publishing
the page again posts a new article either way. Links saved by earlier versions of the package didn't
record their connection; they're checked with your DEV connection if you only have one.

### Reviewing before publishing

Leave **Publish immediately** off and publish on DEV yourself, or add Automate's *Request Approval* step followed by a second *Publish Content to DEV* step with **Publish immediately** on.

## How content is converted

| Content | Becomes |
|---|---|
| Markdown editor | The Markdown you wrote, unchanged. |
| Rich Text | Converted from HTML. Blocks in the editor are rendered by your site's partial views first. Video embeds (YouTube, Vimeo, …) become DEV `{% embed %}` tags. |
| Block List / Block Grid | Each block's properties in order, including nested blocks and grid areas. |
| Textstring / Textarea (in a block) | Text. A Textstring whose alias ends in `heading`, `headline` or `title` becomes a `##` heading. |
| Media picker (in a block) | An image, using the media's `altText` property or its name as alt text. |
| Multi URL picker (in a block) | Links. |
| Code blocks | A block with a `code` (or `codeSnippet`, `snippet`, `sourceCode`, `codeBlock`) property becomes a fenced code block, highlighted using a `language`, `lang`, `codeLanguage` or `syntax` property. |
| Anything else | Left out: toggles, colours, content pickers and so on have no place in an article. |

Relative links and image URLs are made absolute. Code samples are left untouched.

### Custom blocks

If the built-in conversion doesn't suit a block, implement `IDevToBlockConverter` and register it:

```csharp
public class CalloutConverter : IDevToBlockConverter
{
    public string? Convert(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context)
        => content.ContentType.Alias switch
        {
            "callout" => $"> **Note:** {context.ConvertProperty(content, "text")}",
            "newsletterSignup" => "",   // leave this block out
            _ => null,                  // not mine: use the next converter / built-in conversion
        };
}

public class DevToConvertersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<IDevToBlockConverter, CalloutConverter>();
}
```

`DevToConversionContext` gives you the same helpers the built-in conversion uses: `ConvertProperty`, `ConvertElement` (for nested blocks), `HtmlToMarkdown`, `ResolveUrl` and `GetMediaUrl`.

## URLs

DEV needs absolute URLs for the canonical link, links and images. By default the action uses the absolute URL Umbraco generates for the content, which works when the site has a domain assigned (**Culture and Hostnames**) or `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` is set. Otherwise, set **Site URL** on the step, e.g. to `$Umbraco:Automate:Variables:DevTo:SiteUrl`.

## Troubleshooting

API failures are classified so Automate can decide what to do: rate limiting (429), timeouts and DEV being unavailable (5xx) are transient and retried according to the step's error behaviour; an invalid API key, a rejected article (422) or missing settings fail straight away with the DEV error message.

## Migrating from the 1.0.0 betas

The betas (`1.0.0-beta.1` to `beta.3`) used different aliases and configuration paths. Upgrading needs three changes:

- **Configuration.** Move your values to Umbraco Automate's shared sections: `Umbraco:Community:Automate:DevTo:Secrets:ApiKey` becomes `Umbraco:Automate:Secrets:DevTo:ApiKey`, and anything under `Umbraco:Community:Automate:DevTo:Variables` moves to `Umbraco:Automate:Variables:DevTo` (e.g. `SiteUrl`).
- **Connections.** The connection type alias is now `community.devto` (was `devto`), so create your DEV connection again as in [Setup](#setup). Its API key comes pre-filled with the new reference.
- **Automations.** The action alias is now `community.devto.publishContent` (was `devto.publishContent`), so replace the **Publish Content to DEV** step in each automation that uses it.

## Compatibility

| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 1.x | 17.x – 18.x | 17.4 – 18.x |

One build supports both Umbraco 17 and 18: it's compiled against 17, and every change is tested on both, including running the 17 build on 18. Umbraco 19 isn't supported until it has been tested.

## Links

- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/DevTo/Umbraco.Community.Automate.DevTo)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
- [DEV API documentation](https://developers.forem.com/api/v1)
