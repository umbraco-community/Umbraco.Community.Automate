const columnList: UmbExtensionManifest = {
    type: "propertyEditorUi",
    alias: "UmbracoCommunityAutomateGoogleSheets.PropertyEditorUi.ColumnList",
    name: "Google Sheets Column List",
    element: () => import("./column-list.element.js"),
    meta: {
        label: "Column List",
        icon: "icon-sheet",
        group: "Automate",
    },
};

export const columnListManifests: UmbExtensionManifest[] = [columnList];
