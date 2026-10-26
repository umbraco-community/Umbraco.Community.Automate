// Hand-written static web asset, served at /App_Plugins/UmbracoCommunityAutomateSkoda/icons/.
// Registered by wwwroot/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-skoda",
        path: () => import("./skoda.icon.js"),
        keywords: ["skoda", "vehicle", "car"],
    },
];
