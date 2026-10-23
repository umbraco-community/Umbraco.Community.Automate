# Security policy

These packages connect Umbraco sites to external services and handle credentials such as API keys, access tokens and OAuth secrets, so security reports are taken seriously.

## Reporting a vulnerability

**Please don't open a public issue, discussion or pull request for a security problem.** Report it privately instead:

1. Go to the repository's [**Security** tab](https://github.com/umbraco-community/Umbraco.Community.Automate/security) and choose **Report a vulnerability** ([direct link](https://github.com/umbraco-community/Umbraco.Community.Automate/security/advisories/new)).
2. Say which package and version is affected, what the problem is, and how to reproduce it. A proof of concept helps, but please don't include real credentials or data from a live site.

You should get a reply within a few working days. Once the problem is confirmed, a fix is prepared privately, released, and then published as a security advisory crediting you, unless you'd rather not be named.

## What counts

For example:

- A way for a credential stored in configuration or on a connection to be revealed: in logs, error messages, step outputs or backoffice responses.
- A way for a backoffice user to see or use a connection, or run an action, they shouldn't have access to.
- A way for content or input from outside the site to make an action send requests it shouldn't (for example, request forgery through a URL setting).

Problems in Umbraco Automate or Umbraco CMS themselves should go to [Umbraco's security policy](https://github.com/umbraco/Umbraco-CMS/security/policy), and problems in an external service to that service.

## If a credential has leaked

If you've committed or published a real API key, token or OAuth secret, even briefly, **revoke or rotate it at the service straight away**. Deleting it from the repository isn't enough: it stays in git history and may already have been copied. Then remove it from the repository, and let the maintainers know privately if it was pushed here.

This repository's git hooks scan for secrets before each commit and push. See [Preventing secret leaks](CONTRIBUTING.md#preventing-secret-leaks).

## Supported versions

Fixes are released for the latest version of each package. Please update to it before reporting, if you can.
