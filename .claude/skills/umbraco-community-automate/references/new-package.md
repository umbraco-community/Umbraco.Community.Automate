# Building a new package: the guided process

When someone asks for a package ("build me an Automate package for Facebook", "I want a Slack integration"), don't start writing code. Take them through a short series of choices first, the way a good colleague would: find out what the service can do, then let them pick what the package does and how, one step at a time, with a recommendation at each step. Only build once they've confirmed a summary.

This matters because the first request rarely says enough. "A package for Facebook" could mean posting to a Page, reading comments, or reacting to new leads, and each needs different permissions, authentication and settings. Asking costs a minute; building the wrong thing costs an afternoon and a confusing pull request.

## How to ask

- **Ask with multiple-choice questions.** Use the `AskUserQuestion` tool when it's available (it shows options the user picks from, and always allows a free-text answer). Without it, write numbered options in your reply and wait for the answer.
- **Put the recommended option first and mark it "(Recommended)"**, with a one-line reason in its description. Most users will take it, which keeps the process quick.
- **Ask up to four related questions at once**, then wait. Each round should depend on the answers before it, so don't ask everything up front.
- **Use `multiSelect` for "which of these do you want"** (actions, triggers), single choice for "which one" (auth method, shape).
- **Don't ask what you can find out.** Research the service before the first question, read the repo instead of asking about its conventions, and apply the conventions in SKILL.md without asking. Only ask about choices that are genuinely the user's.
- **Keep the user's words in the result.** If they say "post my blog posts to my Facebook Page", that sentence becomes the README's summary and shapes the actions.

## Step 0: Research the service (before asking anything)

Look up the service's API, using web search and its developer documentation, and note:

- **What it can do** that an automation would want: create (post, send, add a row), look up (find, get), update, delete. Pick the handful of operations people would most likely automate, not every endpoint.
- **What can trigger an automation from it**: webhooks the service sends, or nothing (in which case triggers come from Umbraco, not the service).
- **How it authenticates**: API key or token, OAuth (and whether OAuth tokens expire and need refreshing), or none.
- **Hurdles a site owner will hit**: app review or approval (Facebook, Instagram and LinkedIn require an app and often review for posting permissions), business verification, paid tiers, rate limits, required scopes, deprecations.
- **Whether it already exists**: in this repo (`Packages/`), as an open pull request, on NuGet, or as one of Umbraco Automate's built-in steps (listed in SKILL.md under "Packages without a provider"). If a package exists, suggest extending it instead (see "Adding to an existing connection"); if a built-in step already does it, say so before going further. For a general-purpose step with no service, suggest an existing general package that fits before a new one.

Tell the user what you found in a few sentences before the first question, especially any hurdle that could stop the package being useful ("Posting to a Facebook Page needs a Meta app with the `pages_manage_posts` permission, which requires App Review"). If a hurdle makes the package impractical, say so and let them decide whether to continue.

## Step 1: What it should do

Ask, in one round:

1. **Which actions?** (multiSelect) Offer the 3 or 4 most useful operations from your research, the most common first and recommended, e.g. "Post to a Page (Recommended)", "Post a photo", "Find a post", "Delete a post".
2. **Any triggers?** Offer "No triggers (Recommended)" unless the request implies one, plus what the service supports, e.g. "When a new comment arrives (webhook)". Explain that most packages are actions only, and Umbraco's own triggers (Content Published and so on) start the automation.

## Step 2: How it connects and how much it needs

Ask, in one round:

1. **Authentication**, only if the service offers a real choice (e.g. "Page access token (Recommended): simplest, one token in configuration" vs "OAuth sign-in: users connect their own account from the backoffice"). If there's only one way, state it and don't ask.
2. **Shape**: "Simple (Recommended)" vs "Full", with the reason from your answers so far. Recommend Simple unless the choices need something from the Full column in SKILL.md: a trigger, outcomes, several actions sharing request and error handling, or a custom editor. If Full, ask which of those parts they want rather than adding all of them.
3. **Icon**: do the searching first, so the recommended option is ready to use (see Icons in SKILL.md), then always give them the chance to use their own. For a commercial vendor, search the web for its logo as an SVG (its brand kit first, then Simple Icons) and offer "The <vendor> logo from <source> (Recommended)", "My own icon file" and "A built-in Umbraco icon" (name one that fits, e.g. `icon-share`). If no usable SVG turned up, say so and recommend the built-in icon instead. For a utility package with no vendor, offer the built-in icon that fits (Recommended, e.g. `icon-link`) and "My own icon file". Their own icon must be an SVG (a file path or pasted markup); prepare it as SKILL.md describes.

## Step 3: Names

Propose, don't ask open-endedly:

- **Area name** (`Facebook`), which sets the folder `Packages/Facebook/`, the package ID `Umbraco.Community.Automate.Facebook`, the configuration path `Umbraco:Automate:Secrets:Facebook`, and the tag prefix `facebook-v`.

A new package always starts with the community naming: the `Umbraco.Community.Automate.<Area>` package ID and namespaces, and `community.` aliases. Don't offer a personal prefix (`SA.`, `OC.`) as an option. If the user asks for one, recommend the community naming and explain why: it matches every other package, and it's published and maintained as part of the community set. Only someone migrating a package that's already released under their own name has a reason to keep it (see [migrating.md](migrating.md)).
- **Aliases**: `community.facebook` and `community.facebook.<action>`, e.g. `community.facebook.createPagePost`. Point out that aliases are permanent once released.
- **Display names** for the connection and each action, as they'll appear in the backoffice ("Facebook Page", "Post to Facebook Page").

Offer the proposal with "Use these (Recommended)" and "Change them" as options.

## Step 4: Settings for each action

For each action, list the settings you'd give it, with the editor and default you'd use (following [fields.md](fields.md)), and ask whether to change anything. One question per action, or one for all if they're small:

```
Post to Facebook Page
  Message       text area, supports bindings, default "${ trigger.contentName }"
  Link          text box, supports bindings, optional
  Published     toggle, default on ("off" saves it as an unpublished post)
```

Options: "Looks right (Recommended)", "Change something". Settings are where most of the package's usability comes from, so this step is worth the round.

## Step 5: Confirm, then build

Summarise every decision in a short list: what it does, how it connects, shape, names, settings per action, icon, and anything the site owner will have to do at the service (create an app, request permissions). Ask "Build it (Recommended)" or "Change something".

Then build it, following the checklist under "Creating a new connection" in SKILL.md and [simple.md](simple.md) or [full.md](full.md). Only change the files listed under "What a change may touch" in SKILL.md: the package's own folder and its wiring. Don't ask further questions during the build unless something you found contradicts an earlier answer; if it does, explain and ask. At the end, report what you built, what you verified (build, tests, the Demo site), and what the user needs to do next (set up the app at the service, add the key to user secrets, open a pull request).

## Shortcuts

- If the user has already answered some of these in their request ("a Bluesky package with an API key that posts a status"), skip those questions and say what you've assumed.
- If they say "just build it" or "use your judgement", take the recommended option at every step, show the Step 5 summary, and build once they confirm.
- If they ask for a single action on an existing package, skip the process: that's a small change, covered by "Adding to an existing connection" in SKILL.md.
