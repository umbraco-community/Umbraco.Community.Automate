namespace Umbraco.Community.Automate.DevTo.Tests.Helpers;

/// <summary>
/// Locates the package's hand-written static web assets. They have no build step, so nothing
/// copies them to the test output and they have to be read from the project directory.
/// </summary>
internal static class DevToPackagePaths
{
    public static string Wwwroot => Path.Combine(ProjectDirectory, "wwwroot");

    private static string ProjectDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "Umbraco.Community.Automate.DevTo");
                if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
                    return candidate;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the DevTo project directory.");
        }
    }
}
