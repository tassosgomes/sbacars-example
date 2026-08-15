using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.UnitTests;

public class CurrentUserTests
{
    /// <summary>
    /// A minimal, test-only implementation exercising the contract's expected
    /// case-insensitive permission comparison rule (see <see cref="ICurrentUser.HasPermission"/>).
    /// The real implementation, backed by claims, is built in BuildingBlocks.Web (task A3+).
    /// </summary>
    private sealed class FakeCurrentUser : ICurrentUser
    {
        private readonly HashSet<string> _permissions;

        public FakeCurrentUser(bool isAuthenticated, string? userId, string? displayName, params string[] permissions)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
            DisplayName = displayName;
            _permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsAuthenticated { get; }

        public string? UserId { get; }

        public string? DisplayName { get; }

        public IReadOnlyCollection<string> Permissions => _permissions;

        public bool HasPermission(string permission) => _permissions.Contains(permission);
    }

    [Fact]
    public void HasPermission_WhenPermissionIsPresent_ReturnsTrue()
    {
        // Arrange
        var user = new FakeCurrentUser(true, "sub-123", "Ana", "estoque:gerenciar", "estoque:ler");

        // Act & Assert
        user.HasPermission("estoque:gerenciar").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WhenPermissionIsAbsent_ReturnsFalse()
    {
        // Arrange
        var user = new FakeCurrentUser(true, "sub-123", "Ana", "estoque:ler");

        // Act & Assert
        user.HasPermission("estoque:gerenciar").Should().BeFalse();
    }

    [Theory]
    [InlineData("estoque:gerenciar")]
    [InlineData("ESTOQUE:GERENCIAR")]
    [InlineData("Estoque:Gerenciar")]
    public void HasPermission_IsCaseInsensitive(string queried)
    {
        // Arrange: the granted permission is stored in the project's lowercase convention.
        var user = new FakeCurrentUser(true, "sub-123", "Ana", "estoque:gerenciar");

        // Act & Assert
        user.HasPermission(queried).Should().BeTrue();
    }

    [Fact]
    public void Permissions_ForUnauthenticatedUser_IsEmpty()
    {
        // Arrange
        var user = new FakeCurrentUser(false, null, null);

        // Act & Assert
        user.IsAuthenticated.Should().BeFalse();
        user.Permissions.Should().BeEmpty();
        user.HasPermission("estoque:gerenciar").Should().BeFalse();
    }

    [Fact]
    public void Permissions_ExposesGrantedSet_WithoutRoles()
    {
        // Arrange
        var user = new FakeCurrentUser(true, "sub-123", "Ana", "estoque:gerenciar", "estoque:ler");

        // Act & Assert
        user.Permissions.Should().BeEquivalentTo(["estoque:gerenciar", "estoque:ler"]);
    }
}
