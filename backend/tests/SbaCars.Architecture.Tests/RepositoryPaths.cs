namespace SbaCars.Architecture.Tests;

/// <summary>
/// Locates <c>backend/src</c> relative to the test assembly, by walking up from
/// <see cref="AppContext.BaseDirectory"/> until a directory containing <c>SbaCars.sln</c> is
/// found. Most checks in this project scan files on disk (see <see cref="AuthorizationVocabularyTests"/>)
/// so they do not need a compile-time reference to every assembly they police; the B4 contract
/// snapshot gate is the deliberate exception — it reflects over <c>SbaCars.Contracts</c> via a single
/// <c>ProjectReference</c> and still reads the golden <c>schema-snapshot.json</c> from this path.
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

    public static string ContractsSchemaSnapshotPath =>
        Path.Combine(BackendSrcDirectory, "Contracts", "SbaCars.Contracts", "schema-snapshot.json");
}
