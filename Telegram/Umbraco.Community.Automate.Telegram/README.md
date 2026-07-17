# Umbraco Community Automate Telegram

Telegram connection, actions, and notification channel for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

## Overview

Umbraco.Community.Automate.Telegram is a provider package that adds Telegram messaging to Umbraco Automate. It contributes a Telegram connection type (authenticated with a bot token), a **Send Telegram Message** action, and a Telegram notification channel — so an automation can post to a Telegram chat as a workflow step, or automatically when a run fails.

## Key Features

- **Telegram connection type** — stores a bot token (from [@BotFather](https://t.me/BotFather)) and a default chat ID, verified via Telegram's `getMe` API
- **Send Telegram Message action** — sends a MarkdownV2-formatted message to the connection's default chat, with an optional per-step chat ID override
- **Telegram notification channel** — notifies a Telegram chat automatically based on an automation's run-status settings (Failed, Completed, etc.), alongside the built-in Email and Webhook channels
- **Rate-limit aware** — retries once after Telegram's `retry_after` delay on HTTP 429 before failing

## Installation

```bash
dotnet add package Umbraco.Community.Automate.Telegram
```

## Configuration

1. Create a bot with [@BotFather](https://t.me/BotFather) on Telegram and copy the bot token it gives you.
2. Add the bot to the chat, group, or channel you want it to post to, and find that chat's ID (e.g. via `https://api.telegram.org/bot<token>/getUpdates` after sending the bot a message, or a chat-ID lookup bot).
3. Create a Telegram connection in a workspace from the backoffice, paste in the bot token and chat ID, and verify it — this powers the **Send Telegram Message** action.
4. To get failure notifications, open an automation's notification settings and enable the **Telegram** channel, entering the same bot token and chat ID directly — notification channels configure their own credentials, independent of any connection.

## Message formatting

Messages are sent using Telegram's MarkdownV2 parse mode. The **Send Telegram Message** action sends your text as-is — if your message includes literal `_ * [ ] ( ) ~ \` > # + - = | { } . !` characters (including in bound values from earlier steps), escape them with a backslash or Telegram will reject the message. The notification channel escapes the automation name and error details automatically since it builds the message for you.

## Troubleshooting

If a run fails with an authentication error, the bot token is invalid or was revoked — generate a new one with @BotFather and update the connection.

If a run fails saying Telegram couldn't find the chat, double-check the chat ID and make sure the bot has been added to that chat (or has an open conversation with it, for a private chat).

If a run fails with a rate-limiting error, Telegram is throttling this bot — the action already retries once automatically; if it still fails, wait before retrying the run.

## License

MIT — see [LICENSE](../../LICENSE) for details.
