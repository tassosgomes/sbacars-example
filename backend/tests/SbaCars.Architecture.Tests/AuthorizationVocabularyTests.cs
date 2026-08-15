using System.Text.RegularExpressions;

namespace SbaCars.Architecture.Tests;

/// <summary>
/// Enforces the first two of the three non-negotiable rules of §5.6 of the architecture plan:
/// authorization speaks permissions, never roles. A role is a Logto/IdP concept that
/// <see cref="SbaCars.BuildingBlocks.Web.Auth.ScopeClaimsTransformation"/> (in
/// <c>BuildingBlocks.Web</c>) is the only place allowed to translate into the business vocabulary
/// of a permission — everything downstream, including this codebase's own future services, must
/// go through a permission-named policy (<c>[Authorize(Policy = Permissoes.X)]</c>), never
/// <c>[Authorize(Roles = "...")]</c> nor <c>User.IsInRole(...)</c>.
/// </summary>
/// <remarks>
/// <b>Why a source scan instead of reflection (NetArchTest/Mono.Cecil).</b>
/// <c>[Authorize(Roles = "...")]</c> is an attribute and would be checkable by reflecting over
/// compiled assemblies, but <c>.IsInRole(...)</c> is a call inside a method body — invisible to
/// plain reflection, which only sees type-level metadata. Catching both with one mechanism, and
/// without giving this test project a compile-time reference to every service assembly it needs
/// to police (which would only grow as A7–D6 add more), is a plain text scan over
/// <c>backend/src/**/*.cs</c>. It is intentionally coarse: a match inside a string literal or a
/// comment also fails the build, which is the safe direction to be wrong in for a rule this load
/// bearing (§5.6: "role espalhado por atributo de controller... é falha de segurança, não bug de
/// tela"). A9 may expand this project with reflection/IL-based checks for other boundaries
/// (project-reference direction, `DbContext` confined to Infrastructure, etc.); this file is the
/// seed, not the final shape.
/// </remarks>
public sealed class AuthorizationVocabularyTests
{
    private static readonly Regex AuthorizeRolesPattern = new(
        @"\[\s*Authorize\s*\([^\]]*\bRoles\s*=",
        RegexOptions.Compiled);

    private static readonly Regex IsInRolePattern = new(
        @"\.IsInRole\s*\(",
        RegexOptions.Compiled);

    [Fact]
    public void NoSourceFileUnderSrc_UsesAuthorizeRoles()
    {
        var offenders = FindMatches(AuthorizeRolesPattern);

        offenders.Should().BeEmpty(
            "authorization must be expressed as [Authorize(Policy = Permissoes.X)] (§5.6) — " +
            $"found [Authorize(Roles = ...)] in: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void NoSourceFileUnderSrc_CallsIsInRole()
    {
        var offenders = FindMatches(IsInRolePattern);

        offenders.Should().BeEmpty(
            "use case and controller code must ask ICurrentUser.HasPermission, never IsInRole (§5.6) — " +
            $"found .IsInRole(...) in: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Menor número de arquivos que uma varredura saudável de <c>backend/src</c> deve encontrar.
    /// Existe para impedir aprovação vazia: um teste que não varreu arquivo nenhum também não
    /// encontra infração e passaria em silêncio, dando confiança falsa sobre um invariante de
    /// segurança. Se a estrutura do repositório mudar, este teste falha alto em vez de virar
    /// decoração.
    /// </summary>
    private const int MinimumScannedFiles = 20;

    private static List<string> FindMatches(Regex pattern)
    {
        var srcDirectory = RepositoryPaths.BackendSrcDirectory;

        var scanned = 0;
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(srcDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            scanned++;
            var content = File.ReadAllText(file);
            if (pattern.IsMatch(content))
            {
                offenders.Add(Path.GetRelativePath(srcDirectory, file));
            }
        }

        scanned.Should().BeGreaterThanOrEqualTo(
            MinimumScannedFiles,
            $"the scan of '{srcDirectory}' must actually reach the source files; " +
            "an empty scan finds no violations and would pass without guarding anything");

        return offenders;
    }
}
