// Hand-written static web asset, served at /App_Plugins/UmbracoCommunityAutomateMastodon/icons/.
// Registered by wwwroot/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-mastodon",
        path: () => import("./mastodon.icon.js"),
        keywords: ["mastodon", "fediverse", "social", "toot", "post"],
    },
];
