using System.Text.RegularExpressions;

namespace SbaCars.Architecture.Tests;

/// <summary>
/// Reads the project-reference graph of everything under <c>backend/src</c> straight from
/// <c>.csproj</c> XML text — the same "scan the file on disk" mechanic
/// <see cref="RepositoryPaths"/>-based tests already use, rather than loading compiled
/// assemblies. The graph is deterministic and cheap to build (a few dozen small XML files), and
/// reading it directly is what proves the SBA service boundary (§2.4, §3.1 of the architecture
/// plan) without giving this test project a compile-time reference to every service assembly it
/// polices — a reference this project would otherwise have to grow every time a fifth service
/// joined the solution.
/// </summary>
/// <remarks>
/// Also exposes, per project, whether it directly declares a dependency on EF Core or ASP.NET
/// Core (package reference, SDK, or <c>FrameworkReference</c>) — reused by
/// <see cref="LayerPurityTests"/> to walk the *transitive* closure of a <c>.Domain</c>/
/// <c>.Application</c> project and prove neither framework is reachable through any chain of
/// <c>ProjectReference</c>s, not just a direct one.
/// </remarks>
internal sealed class ProjectReferenceGraph
{
    private static readonly Regex ProjectReferencePattern = new(
        @"<ProjectReference\s+Include\s*=\s*""(?<path>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex PackageReferencePattern = new(
        @"<PackageReference\s+Include\s*=\s*""(?<name>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex FrameworkReferencePattern = new(
        @"<FrameworkReference\s+Include\s*=\s*""(?<name>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex SdkPattern = new(
        @"<Project\s+Sdk\s*=\s*""(?<sdk>[^""]+)""",
        RegexOptions.Compiled);

    private readonly Dictionary<string, ProjectInfo> _projects;
    private readonly string _srcDirectory;

    private ProjectReferenceGraph(Dictionary<string, ProjectInfo> projects, string srcDirectory)
    {
        _projects = projects;
        _srcDirectory = srcDirectory;
    }

    public IReadOnlyCollection<string> AllProjectPaths => _projects.Keys;

    public static ProjectReferenceGraph Build(string srcDirectory)
    {
        var projects = new Dictionary<string, ProjectInfo>(StringComparer.Ordinal);

        foreach (var csprojPath in Directory.EnumerateFiles(srcDirectory, "*.csproj", SearchOption.AllDirectories))
        {
            var fullPath = Path.GetFullPath(csprojPath);
            var content = File.ReadAllText(fullPath);
            var directory = Path.GetDirectoryName(fullPath)!;

            var references = ProjectReferencePattern.Matches(content)
                .Select(m => ResolveReferencePath(directory, m.Groups["path"].Value))
                .ToArray();

            var packages = PackageReferencePattern.Matches(content)
                .Select(m => m.Groups["name"].Value)
                .ToArray();

            var frameworkReferences = FrameworkReferencePattern.Matches(content)
                .Select(m => m.Groups["name"].Value)
                .ToArray();

            var sdkMatch = SdkPattern.Match(content);
            var sdk = sdkMatch.Success ? sdkMatch.Groups["sdk"].Value : string.Empty;

            projects[fullPath] = new ProjectInfo(references, packages, frameworkReferences, sdk);
        }

        return new ProjectReferenceGraph(projects, Path.GetFullPath(srcDirectory));
    }

    /// <summary>
    /// The first path segment under <c>src/</c> — <c>Catalog</c>, <c>Inventory</c>, <c>Interest</c>,
    /// <c>Purchase</c>, <c>Gateways</c>, <c>BuildingBlocks</c> or <c>Contracts</c>. This is the unit
    /// §3.1 calls a "service" (for the four domain groups) or a shared group (BuildingBlocks,
    /// Contracts, and the gateways-only Gateways group).
    /// </summary>
    public string Group(string projectPath)
    {
        var relative = Path.GetRelativePath(_srcDirectory, projectPath);
        return relative.Split(Path.DirectorySeparatorChar)[0];
    }

    public IReadOnlyList<string> DirectReferences(string projectPath) =>
        _projects.TryGetValue(projectPath, out var info) ? info.References : [];

    /// <summary>
    /// Every project reachable from <paramref name="projectPath"/> by following one or more
    /// <c>ProjectReference</c> edges — never includes <paramref name="projectPath"/> itself.
    /// </summary>
    public IEnumerable<string> TransitiveReferences(string projectPath)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(DirectReferences(projectPath));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var next in DirectReferences(current))
            {
                if (!visited.Contains(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited;
    }

    public bool DeclaresEfCore(string projectPath)
    {
        if (!_projects.TryGetValue(projectPath, out var info))
        {
            return false;
        }

        return info.Packages.Any(package =>
            package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            package.Equals("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal) ||
            package.Equals("EFCore.NamingConventions", StringComparison.Ordinal));
    }

    public bool DeclaresAspNetCore(string projectPath)
    {
        if (!_projects.TryGetValue(projectPath, out var info))
        {
            return false;
        }

        return info.Sdk.Equals("Microsoft.NET.Sdk.Web", StringComparison.Ordinal) ||
            info.FrameworkReferences.Contains("Microsoft.AspNetCore.App", StringComparer.Ordinal) ||
            info.Packages.Any(package => package.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    private static string ResolveReferencePath(string fromDirectory, string include)
    {
        // .csproj files in this repo always write ProjectReference paths with Windows-style
        // backslashes (MSBuild accepts either, but every file here was authored that way) — swap
        // them for the platform separator before combining, or Path.GetFullPath would treat the
        // whole relative path as a single (nonexistent) file name on Linux/macOS CI runners.
        var normalized = include.Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(fromDirectory, normalized));
    }

    private sealed record ProjectInfo(
        IReadOnlyList<string> References,
        IReadOnlyList<string> Packages,
        IReadOnlyList<string> FrameworkReferences,
        string Sdk);
}
