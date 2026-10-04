# Umbraco.Community.Automate

A home for community-built packages for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate), and a starter kit for building your own.

Umbraco Automate adds an **Automation** section to the Umbraco backoffice, where you build workflows from a *trigger* (for example "content published") and *actions* (for example "post to Mastodon"). Each package in this repo adds connection types, actions or triggers for one service. The repo gives you everything you need to add another one:

- **The `umbraco-automate` skill** for [Claude Code](https://claude.com/claude-code), which builds new packages and migrates existing ones to the repo's conventions.
- **[Packages](#packages)** to use as they are, or to learn from.
- **[Two reference examples](#examples-and-development-tools)** that the skill builds from (one simple, one with everything), and a Test Connection action for checking connections.
- **[A Demo site](#running-the-demo-site)** with every package installed, to try them in a real backoffice.
- **CI and releases** that pick up a new package from its folder, with no workflow to edit.

## Building a package with the skill

**We recommend using the skill to create every new package and to migrate every existing one.** It builds them all the same way, so every package has the same structure, configuration and tests, and anyone who knows one package can find their way around the rest.

### Install it

The skill lives in this repo at [`.claude/skills/umbraco-automate/`](.claude/skills/umbraco-automate/SKILL.md), so there's nothing to install apart from Claude Code itself. It's written for this repo's layout, so use it in a clone:

1. Install [Claude Code](https://claude.com/claude-code).
2. [Fork this repo](https://github.com/umbraco-community/Umbraco.Community.Automate/fork) and clone your fork.
3. Start Claude Code in the repo's root folder (`claude` in a terminal, or open the folder in VS Code or JetBrains with the Claude Code extension).

Claude Code loads the skill whenever you ask for something Automate-related. You can also call it by name with `/umbraco-automate`.

### Create a package

Ask for the package you want, for example:

> Using the Umbraco Automate skill, create me a package for the PokéAPI, with an action that looks up a Pokémon by name.

The skill researches the service first. Then it walks you through the choices one at a time, recommending an option each time: which actions and triggers, how it connects, the names, and the settings for each action. When you confirm a summary, it builds the package with tests, wires it into the solution and the Demo site, and writes the package's README.

### Migrate a package

To bring in a package you already publish, ask it to:

> Using the Umbraco Automate skill, migrate my package from `<path or repo URL>`.

It reads your code and asks the questions that affect your existing users, then reshapes the package to match the others.

We recommend moving to the community naming (`Umbraco.Community.Automate.<Area>` package ID and namespaces, `community.` aliases), but you can keep your own package ID, namespaces and aliases for compatibility, so your existing users can upgrade without reinstalling or rebuilding their automations. Either way, the package follows the rest of the repo's conventions.

### Change an existing package

For smaller jobs, ask in the same way: "Using the Umbraco Automate skill, add a trigger to Mastodon". The skill follows the same conventions.

Not using Claude Code? [Adding a new package](.github/CONTRIBUTING.md#adding-a-new-package) in CONTRIBUTING has the same process as a checklist, and the [examples](#examples-and-development-tools) show what a finished package looks like.

## Packages

Every package supports Umbraco 17 and 18 (with Umbraco Automate 17 and 18) from one build. Each package's README explains how to install and set it up, including its configuration, all its settings, and troubleshooting.

| Package | What it does | Status |
|---|---|---|
| [DevTo](Packages/DevTo/Umbraco.Community.Automate.DevTo/README.md) | Cross-posts Umbraco content to [DEV Community](https://dev.to). Converts Markdown, Rich Text, Block List and Block Grid to Markdown, and sets the canonical URL to your site. | [![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.Automate.DevTo?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.Automate.DevTo) pre-release |
| [GoogleSheets](Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets/README.md) | Connects to Google Sheets with OAuth. Appends, finds, updates, deletes and upserts rows; reads and clears ranges; creates spreadsheets and tabs. | Not yet on NuGet |
| [Mastodon](Packages/Mastodon/Umbraco.Community.Automate.Mastodon/README.md) | Posts statuses to any Mastodon instance, with visibility, content warnings and an appended URL. Replaces `OC.Automate.Mastodon`. | Not yet on NuGet |
| [Pushover](Packages/Pushover/Umbraco.Community.Automate.Pushover/README.md) | Sends push notifications to phones and desktops through [Pushover](https://pushover.net/), with titles, links, sounds and priorities, including emergency alerts that repeat until acknowledged. Replaces `SA.Automate.Pushover`. | Not yet on NuGet |
| [Skoda](Packages/Skoda/Umbraco.Community.Automate.Skoda/README.md) | Reads a Škoda vehicle's status and controls charging, air conditioning, auxiliary heating and ventilation through the MyŠkoda Public API. | In development |
| [WeatherApi](Packages/WeatherApi/Umbraco.Community.Automate.WeatherApi/README.md) | Gets the current weather or today's forecast for a location from [WeatherAPI.com](https://www.weatherapi.com/). Replaces `SA.Automate.WeatherApi`. | Not yet on NuGet under this name |

To use a package before it's on NuGet, see [Using a package before it's released](.github/CONTRIBUTING.md#using-a-package-before-its-released).

### Examples and development tools

`Packages/_Development/` holds what helps you build and test packages. **None of it is for use in a real site**, and none of it is published, but CI builds and tests it so it always matches the current conventions.

- [**Simple**](Packages/_Development/Simple/Umbraco.Community.Automate.Examples.Simple/README.md): a reference example with an API key and one action, the shape most packages need. The skill builds new packages from it.
- [**Kitchen Sink**](Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/README.md): a reference example with every convention in one place: a connection, two actions, a trigger, outcomes, an API client, a backoffice front end and tests.
- [**Test Connection**](Packages/_Development/TestConnection/Umbraco.Community.Automate.TestConnection/README.md): a helper action, not a package for real sites. It runs a connection's **Test connection** check from an automation, so you can confirm you've put the right credentials in and that automations using the connection will actually work. The Demo site's test pages use it.

Both examples only talk to [httpbin.org](https://httpbin.org), a test service that echoes requests back, so they work in the Demo site with no signup.

## Running the Demo site

`Umbraco.Community.Automate.Demo/` is a throwaway Umbraco site that references every package and example. It installs itself into a local SQLite database the first time it runs, so there's no database server or setup screen. You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js 22](https://nodejs.org/) and a trusted dev certificate (`dotnet dev-certs https --trust`).

```bash
# Backoffice front ends are generated, not committed: build them once
cd Packages/GoogleSheets/Umbraco.Community.Automate.GoogleSheets/Client
npm ci && npm run build
cd ../../../..
cd Packages/_Development/KitchenSink/Umbraco.Community.Automate.Examples.KitchenSink/Client
npm ci && npm run build
cd ../../../../..

dotnet run --project Umbraco.Community.Automate.Demo
```

Or open `Umbraco.Community.Automate.Demo.slnx` in Visual Studio, Rider or VS Code and press F5.

Open <https://localhost:44343/umbraco> and log in as `admin@example.com` / `password1234`. The **Automation** section is in the top navigation: connections are under **Settings → Connections**, and automations under **Automations**.

The site sets itself up: every package has a connection ready, using placeholder credentials. Under **Content**, *Automate tests* has a page per connection: publish one and its *Test:* automation checks the connection and tells you on screen whether it works. The Simple and Kitchen Sink examples pass straight away; the others pass once you add real credentials. To try a package against the real service, copy `Umbraco.Community.Automate.Demo/appsettings.Local.example.json` to `appsettings.Local.json` (git-ignored, so it can't be committed), fill in the values for that package, and restart the site. The package's README explains where to get them. [Running the Demo site](.github/CONTRIBUTING.md#running-the-demo-site) in CONTRIBUTING covers user secrets, resetting the site and the end-to-end tests.

## Contributing

Bug reports, fixes and new packages are all welcome. [CONTRIBUTING](.github/CONTRIBUTING.md) covers the repo layout, building and testing, adding a package, and how releases work.

Found a problem, or have an idea for a package? [Open an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues/new/choose). For a security problem, please follow [SECURITY.md](.github/SECURITY.md) and report it privately instead.

## License

MIT. See [LICENSE](LICENSE) for details.
