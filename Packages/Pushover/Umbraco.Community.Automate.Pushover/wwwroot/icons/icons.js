// Hand-written static web asset, served at /App_Plugins/UmbracoCommunityAutomatePushover/icons/.
// Registered by wwwroot/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-pushover",
        path: () => import("./pushover.icon.js"),
        keywords: ["pushover", "notification", "push"],
    },
];
