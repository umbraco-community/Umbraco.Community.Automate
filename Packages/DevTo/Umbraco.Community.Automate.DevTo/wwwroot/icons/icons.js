// Hand-written static web asset, served at /App_Plugins/UmbracoCommunityAutomateDevTo/icons/.
// Registered by wwwroot/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-devto",
        path: () => import("./devto.icon.js"),
        keywords: ["dev", "devto", "dev.to", "forem", "blog", "article"],
    },
];
