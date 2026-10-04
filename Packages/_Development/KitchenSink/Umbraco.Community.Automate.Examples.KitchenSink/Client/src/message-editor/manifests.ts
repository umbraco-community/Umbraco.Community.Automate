// The alias here is what a C# setting uses as EditorUiAlias (see SendMessageSettings.Message).
const messageEditor: UmbExtensionManifest = {
    type: "propertyEditorUi",
    alias: "UmbracoCommunityAutomateExamplesKitchenSink.PropertyEditorUi.Message",
    name: "Example Message Editor",
    element: () => import("./message-editor.element.js"),
    meta: {
        label: "Example Message",
        icon: "icon-autofill",
        group: "Automate",
    },
};

export const messageEditorManifests: UmbExtensionManifest[] = [messageEditor];
