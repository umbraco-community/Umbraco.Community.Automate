using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace Umbraco.Community.Automate.DevTo.Composers;

public class DevToPackageManifestReader : IPackageManifestReader
{
    // Matches StaticWebAssetBasePath in the csproj.
    private const string AppPluginPath = "/App_Plugins/UmbracoCommunityAutomateDevTo";

    public const string BodyPropertiesEditorUiAlias = "UmbracoCommunityAutomateDevTo.PropertyEditorUi.BodyProperties";

    // Matches body-properties.element.js.
    public const string PreviewModalAlias = "UmbracoCommunityAutomateDevTo.Modal.Preview";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var version = typeof(DevToPackageManifestReader).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        return Task.FromResult<IEnumerable<PackageManifest>>(new[]
        {
            new PackageManifest
            {
                Id = "Umbraco.Community.Automate.DevTo",
                Name = "Umbraco Community Automate DEV",
                Version = version,
                AllowTelemetry = true,

                Extensions =
                [
                    new
                    {
                        type = "icons",
                        alias = "UmbracoCommunityAutomateDevTo.Icons",
                        name = "DEV Icons",
                        js = $"{AppPluginPath}/icons.js",
                    },
                    new
                    {
                        type = "localization",
                        alias = "UmbracoCommunityAutomateDevTo.Localization.En",
                        name = "DEV English",
                        meta = new { culture = "en" },
                        js = $"{AppPluginPath}/lang/en.js",
                    },
                    new
                    {
                        type = "propertyEditorUi",
                        alias = BodyPropertiesEditorUiAlias,
                        name = "DEV Body Properties",
                        element = $"{AppPluginPath}/body-properties.element.js",
                        meta = new
                        {
                            label = "DEV Body Properties",
                            icon = "icon-automate-devto",
                            group = "Automate",
                        },
                    },
                    new
                    {
                        type = "modal",
                        alias = PreviewModalAlias,
                        name = "DEV Preview",
                        element = $"{AppPluginPath}/preview-modal.element.js",
                    },
                    new
                    {
                        type = "workspaceInfoApp",
                        alias = "UmbracoCommunityAutomateDevTo.WorkspaceInfoApp.Document.ArticleLinks",
                        name = "DEV Article Links",
                        element = $"{AppPluginPath}/article-links.element.js",
                        weight = 90,
                        conditions = new[] { new { alias = "Umb.Condition.WorkspaceAlias", match = "Umb.Workspace.Document" } },
                    },
                ]
            }
        });
    }
}
