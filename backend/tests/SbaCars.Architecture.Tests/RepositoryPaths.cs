namespace SbaCars.Architecture.Tests;

/// <summary>
/// Locates <c>backend/src</c> relative to the test assembly, by walking up from
/// <see cref="AppContext.BaseDirectory"/> until a directory containing <c>SbaCars.sln</c> is
/// found. Source-level checks in this project (see <see cref="AuthorizationVocabularyTests"/>)
/// scan files on disk rather than loading compiled assemblies, so they need this instead of a
/// project/assembly reference.
/// </summary>
internal static class RepositoryPaths
{
    public static string BackendSrcDirectory
    {
        get
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
}
