// Hand-written, not build output: this package has no Client/ toolchain, so these
// files ship to /App_Plugins/UmbracoCommunityAutomateMastodon/ as static web assets.
// Registered by MastodonPackageManifestReader.
export default [
    {
        name: "icon-mastodon",
        path: () => import("./mastodon.icon.js"),
        keywords: ["mastodon", "fediverse", "social", "toot", "post"],
    },
];
