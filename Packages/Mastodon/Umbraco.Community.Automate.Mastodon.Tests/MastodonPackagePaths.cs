namespace Umbraco.Community.Automate.Mastodon.Tests;

/// <summary>
/// Locates the package's hand-written static web assets. They have no build step, so nothing
/// copies them to the test output and they have to be read from the project directory.
/// </summary>
internal static class MastodonPackagePaths
{
    public static string Wwwroot => Path.Combine(ProjectDirectory, "wwwroot");

    private static string ProjectDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.Mastodon");
                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the Mastodon project directory.");
        }
    }
}
