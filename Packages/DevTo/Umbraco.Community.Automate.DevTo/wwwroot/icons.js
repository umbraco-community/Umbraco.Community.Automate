// Hand-written, not build output: this package has no Client/ toolchain, so these
// files ship to /App_Plugins/UmbracoCommunityAutomateDevTo/ as static web assets.
// Registered by DevToPackageManifestReader.
export default [
    {
        name: "icon-automate-devto",
        path: () => import("./devto.icon.js"),
        keywords: ["dev", "devto", "dev.to", "forem", "blog", "article"],
    },
];
