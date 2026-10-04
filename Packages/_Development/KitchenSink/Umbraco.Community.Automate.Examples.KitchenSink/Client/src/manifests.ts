import { messageEditorManifests } from "./message-editor/manifests.js";

// Front-end extensions built by Vite, loaded by the bundle in public/umbraco-package.json.
// Icons are plain files in public/icons, registered directly by public/umbraco-package.json.
export const manifests: Array<UmbExtensionManifest> = [...messageEditorManifests];
