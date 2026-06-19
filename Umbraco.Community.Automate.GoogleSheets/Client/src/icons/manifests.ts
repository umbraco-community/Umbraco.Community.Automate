const icons: UmbExtensionManifest = {
    type: "icons",
    alias: "UmbracoCommunityAutomateGoogleSheets.Icons",
    name: "Google Sheets Icons",
    js: () => import("./icons.js"),
};

export const iconManifests: UmbExtensionManifest[] = [icons];
