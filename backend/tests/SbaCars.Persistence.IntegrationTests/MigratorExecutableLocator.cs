namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Finds a Migrator project's own built <c>.dll</c> on disk, so <see cref="MigratorExecutableTests"/>
/// can launch the real executable (<c>dotnet &lt;path&gt;</c>) instead of calling
/// <c>Database.MigrateAsync()</c> in-process.
/// </summary>
/// <remarks>
/// Deliberately does not use a <c>ProjectReference</c> to the Migrator project to obtain this path:
/// an <c>OutputType=Exe</c> project referenced by a class library copies its assembly but not
/// reliably its own <c>.runtimeconfig.json</c>/<c>.deps.json</c> alongside it in every SDK version,
/// and <c>dotnet &lt;dll&gt;</c> needs both next to the DLL to run. Walking to the Migrator's own
/// <c>bin/</c> — the exact same output <c>dotnet build</c> already produced for the gate's build
/// step — sidesteps that entirely and matches what the real deploy pipeline runs (§4.3, §11.5).
/// </remarks>
internal static class MigratorExecutableLocator
{
    public static string Resolve(string migratorProjectName)
    {
        var srcDirectory = ResolveBackendSrcDirectory();

        var candidates = Directory
            .EnumerateFiles(srcDirectory, $"{migratorProjectName}.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException(
                $"Could not find a built '{migratorProjectName}.dll' under '{srcDirectory}'. " +
                "Build the solution before running this test — the gate always does.");
        }

        return candidates[0];
    }

    /// <summary>
    /// Walks up from the test assembly's output directory until it finds <c>SbaCars.sln</c>, the
    /// same way <c>SbaCars.Architecture.Tests.RepositoryPaths</c> does — duplicated rather than
    /// shared because the two test projects have no common project reference to hang a shared
    /// helper off, and a five-line directory walk is cheaper than introducing one for this alone.
    /// </summary>
    private static string ResolveBackendSrcDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SbaCars.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                $"Could not locate 'backend/SbaCars.sln' by walking up from '{AppContext.BaseDirectory}'.");
        }

        return Path.Combine(directory.FullName, "src");
    }
}
