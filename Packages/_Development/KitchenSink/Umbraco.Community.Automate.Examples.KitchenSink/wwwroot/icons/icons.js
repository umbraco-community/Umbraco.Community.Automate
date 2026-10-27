// Hand-written static web asset, served at /App_Plugins/UmbracoCommunityAutomateExamplesKitchenSink/icons/.
// Registered by wwwroot/umbraco-package.json. Icon names must match the C# attributes' Icon.
export default [
    {
        name: "icon-automate-kitchensink",
        path: () => import("./kitchensink.icon.js"),
        keywords: ["example", "httpbin", "automate"],
    },
];
