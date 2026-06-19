import { columnListManifests } from "./column-list/manifests.js";
import { iconManifests } from "./icons/manifests.js";

export const manifests: Array<UmbExtensionManifest> = [...columnListManifests, ...iconManifests];
