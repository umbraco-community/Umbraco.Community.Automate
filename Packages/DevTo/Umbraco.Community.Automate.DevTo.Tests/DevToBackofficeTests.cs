using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.DevTo.Actions;
using Umbraco.Community.Automate.DevTo.Composers;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Controllers;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests;

/// <summary>
/// The backoffice side of the package. Mistakes here fail silently in the backoffice (no icon,
/// a plain text box, a description with a hole in it), so these tests are the check.
/// </summary>
public class DevToBackofficeTests
{
    private static string[] RegisteredIconNames()
        => Regex.Matches(File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToArray();

    public static TheoryData<Type> IconOwners => new()
    {
        typeof(DevToConnectionType),
        typeof(PublishContentAction),
    };

    private static async Task<JsonElement> GetExtensionAsync(string type)
    {
        var manifest = Assert.Single(await new DevToPackageManifestReader().ReadPackageManifestsAsync());
        return Assert.Single(JsonSerializer.SerializeToElement(manifest.Extensions).EnumerateArray(),
            e => e.GetProperty("type").GetString() == type);
    }

    [Theory]
    [MemberData(nameof(IconOwners))]
    public void Uses_an_icon_that_icons_js_registers(Type type)
    {
        var icon = type.GetCustomAttribute<ConnectionTypeAttribute>()?.Icon ?? type.GetCustomAttribute<ActionAttribute>()?.Icon;

        Assert.Contains(icon, RegisteredIconNames());
    }

    [Fact]
    public async Task Icons_extension_points_at_files_that_exist()
    {
        var js = (await GetExtensionAsync("icons")).GetProperty("js").GetString()!;

        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateDevTo/icons.js", js);
        Assert.True(File.Exists(Path.Combine(DevToPackagePaths.Wwwroot, "icons.js")));
        Assert.True(File.Exists(Path.Combine(DevToPackagePaths.Wwwroot, "devto.icon.js")));
    }

    [Fact]
    public async Task Body_properties_editor_is_registered_and_its_module_exists()
    {
        var editor = await GetExtensionAsync("propertyEditorUi");

        Assert.Equal(DevToPackageManifestReader.BodyPropertiesEditorUiAlias, editor.GetProperty("alias").GetString());

        var element = editor.GetProperty("element").GetString()!;
        Assert.StartsWith("/App_Plugins/UmbracoCommunityAutomateDevTo/", element);
        Assert.True(File.Exists(Path.Combine(DevToPackagePaths.Wwwroot, Path.GetFileName(element))), $"Missing wwwroot/{Path.GetFileName(element)}");
    }

    [Fact]
    public void Body_properties_setting_uses_the_registered_editor()
    {
        var field = typeof(PublishContentSettings).GetProperty(nameof(PublishContentSettings.BodyProperties))!
            .GetCustomAttribute<EditableModelFieldAttribute>();

        Assert.Equal(DevToPackageManifestReader.BodyPropertiesEditorUiAlias, field?.EditorUiAlias);
    }

    [Fact]
    public void Body_properties_editor_module_defines_the_element_it_exports()
    {
        var js = File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, "body-properties.element.js"));

        Assert.Contains("customElements.define(\"ua-devto-body-properties\"", js);
        Assert.Contains("export { UaDevToBodyPropertiesElement as element }", js);
    }

    [Fact]
    public async Task Preview_modal_is_registered_under_the_alias_the_editor_opens()
    {
        var modal = await GetExtensionAsync("modal");
        Assert.Equal(DevToPackageManifestReader.PreviewModalAlias, modal.GetProperty("alias").GetString());

        var element = modal.GetProperty("element").GetString()!;
        var js = File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, Path.GetFileName(element)));
        Assert.Contains("customElements.define(\"ua-devto-preview-modal\"", js);
        Assert.Contains("export { UaDevToPreviewModalElement as element }", js);

        var editor = File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, "body-properties.element.js"));
        Assert.Contains($"PREVIEW_MODAL_ALIAS = \"{DevToPackageManifestReader.PreviewModalAlias}\"", editor);
    }

    /// <summary>The backoffice builds its URLs by hand, so check each one matches its controller route.</summary>
    [Fact]
    public void Backoffice_calls_match_the_controller_routes()
    {
        var controller = typeof(DevToController);
        var route = controller.GetCustomAttribute<VersionedApiBackOfficeRouteAttribute>()!.Template
            .Replace("[umbracoBackOffice]", "umbraco")
            .Replace("{version:apiVersion}", "1");
        string Template<TAttribute>(string method) where TAttribute : HttpMethodAttribute
            => controller.GetMethod(method)!.GetCustomAttribute<TAttribute>()!.Template!;
        string Js(string file) => File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, file));

        Assert.Contains($"url: `/{route}/${{path}}`", Js("api.js"));

        Assert.Equal("preview", Template<HttpPostAttribute>(nameof(DevToController.Preview)));
        Assert.Contains("devToApi(this, \"post\", \"preview\"", Js("preview-modal.element.js"));

        var links = Js("article-links.element.js");
        Assert.Equal("article-links/{contentKey:guid}", Template<HttpGetAttribute>(nameof(DevToController.GetArticleLinks)));
        Assert.Equal("article-links/{contentKey:guid}", Template<HttpDeleteAttribute>(nameof(DevToController.RemoveArticleLink)));
        Assert.Equal("article-links/{contentKey:guid}/check", Template<HttpPostAttribute>(nameof(DevToController.CheckArticleLinks)));
        Assert.Contains("`article-links/${this.#unique}${suffix}`", links);
        Assert.Contains("this.#request(\"post\", \"/check\")", links);
    }

    [Fact]
    public void Settings_the_editor_sends_to_the_preview_exist()
    {
        var js = File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, "body-properties.element.js"));
        var sent = Regex.Match(js, @"PREVIEW_SETTINGS = \[([^\]]*)\]").Groups[1].Value.Split(',').Select(s => s.Trim(' ', '"')).ToArray();

        Assert.NotEmpty(sent);
        foreach (var alias in sent)
        {
            Assert.NotNull(typeof(PublishContentSettings).GetProperty(alias, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
            Assert.NotNull(typeof(DevToPreviewRequest).GetProperty(alias, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
        }
    }

    [Fact]
    public async Task Article_links_info_app_is_registered_on_documents()
    {
        var app = await GetExtensionAsync("workspaceInfoApp");
        var condition = Assert.Single(app.GetProperty("conditions").EnumerateArray());
        Assert.Equal("Umb.Condition.WorkspaceAlias", condition.GetProperty("alias").GetString());
        Assert.Equal("Umb.Workspace.Document", condition.GetProperty("match").GetString());

        var js = File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, Path.GetFileName(app.GetProperty("element").GetString()!)));
        Assert.Contains("customElements.define(\"ua-devto-article-links\"", js);
        Assert.Contains("export { UaDevToArticleLinksElement as element }", js);

    }

    [Fact]
    public async Task Localization_extension_points_at_a_file_that_exists()
    {
        var js = (await GetExtensionAsync("localization")).GetProperty("js").GetString()!;

        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateDevTo/lang/en.js", js);
        Assert.True(File.Exists(Path.Combine(DevToPackagePaths.Wwwroot, "lang", "en.js")));
    }

    public static TheoryData<Type> SettingsTypes => new()
    {
        typeof(PublishContentSettings),
        typeof(Settings.DevToConnectionSettings),
    };

    /// <summary>
    /// A group heading renders as #uaFieldGroups_{group}Label, which shows as that raw key unless
    /// Automate (only "advanced") or lang/en.js has a label for it.
    /// </summary>
    [Theory]
    [MemberData(nameof(SettingsTypes))]
    public void Every_settings_group_has_a_label(Type settingsType)
    {
        var labels = File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, "lang", "en.js"));

        foreach (var group in settingsType.GetProperties()
                     .Select(p => p.GetCustomAttribute<EditableModelFieldAttribute>()?.Group)
                     .Where(g => !string.IsNullOrEmpty(g) && g != "Advanced")
                     .Distinct())
        {
            var key = char.ToLowerInvariant(group![0]) + group[1..] + "Label";
            Assert.True(Regex.IsMatch(labels, $@"\b{key}\s*:"), $"{settingsType.Name} uses group '{group}' but lang/en.js has no {key}.");
        }
    }

    /// <summary>
    /// Descriptions render as Umbraco Flavored Markdown, where <c>${ … }</c> is an expression and
    /// silently renders as nothing. Binding examples have to sit in a `code span`.
    /// </summary>
    [Theory]
    [MemberData(nameof(SettingsTypes))]
    public void Binding_examples_in_descriptions_are_in_code_spans(Type settingsType)
    {
        foreach (var property in settingsType.GetProperties())
        {
            var description = property.GetCustomAttribute<EditableModelFieldAttribute>()?.Description ?? string.Empty;
            var withoutCodeSpans = Regex.Replace(description, "`[^`]*`", string.Empty);

            Assert.False(withoutCodeSpans.Contains("${"),
                $"{settingsType.Name}.{property.Name}'s description has a ${{ }} example outside a `code span`, which the backoffice renders as blank.");
        }
    }
}
