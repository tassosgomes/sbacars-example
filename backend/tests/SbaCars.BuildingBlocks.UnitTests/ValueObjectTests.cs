using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.UnitTests;

public class ValueObjectTests
{
    private sealed class Endereco : ValueObject
    {
        public Endereco(string rua, string cidade, string? complemento = null)
        {
            Rua = rua;
            Cidade = cidade;
            Complemento = complemento;
        }

        public string Rua { get; }

        public string Cidade { get; }

        public string? Complemento { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Rua;
            yield return Cidade;
            yield return Complemento;
        }
    }

    private sealed class Coordenada : ValueObject
    {
        public Coordenada(decimal latitude, decimal longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public decimal Latitude { get; }

        public decimal Longitude { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
        }
    }

    [Fact]
    public void Equals_WithSameComponents_ReturnsTrue()
    {
        // Arrange
        var first = new Endereco("Rua A", "São Paulo");
        var second = new Endereco("Rua A", "São Paulo");

        // Act & Assert
        first.Equals(second).Should().BeTrue();
        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentComponents_ReturnsFalse()
    {
        // Arrange
        var first = new Endereco("Rua A", "São Paulo");
        var second = new Endereco("Rua B", "São Paulo");

        // Act & Assert
        first.Equals(second).Should().BeFalse();
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Equals_IsSensitiveToComponentOrder()
    {
        // Arrange: same two values, swapped between latitude and longitude.
        var first = new Coordenada(latitude: 1m, longitude: 2m);
        var second = new Coordenada(latitude: 2m, longitude: 1m);

        // Act & Assert
        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNullComponent_TreatsItConsistently()
    {
        // Arrange
        var first = new Endereco("Rua A", "São Paulo", complemento: null);
        var second = new Endereco("Rua A", "São Paulo", complemento: null);
        var third = new Endereco("Rua A", "São Paulo", complemento: "Apto 1");

        // Act & Assert
        first.Equals(second).Should().BeTrue();
        first.Equals(third).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var endereco = new Endereco("Rua A", "São Paulo");
        var coordenada = new Coordenada(1m, 2m);

        // Act & Assert
        endereco.Equals(coordenada).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var endereco = new Endereco("Rua A", "São Paulo");

        // Act & Assert
        endereco.Equals(null).Should().BeFalse();
        (endereco == null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ForEqualValueObjects_IsTheSame()
    {
        // Arrange
        var first = new Endereco("Rua A", "São Paulo");
        var second = new Endereco("Rua A", "São Paulo");

        // Act & Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ForDifferentValueObjects_Differs()
    {
        // Arrange
        var first = new Endereco("Rua A", "São Paulo");
        var second = new Endereco("Rua B", "Campinas");

        // Act & Assert
        first.GetHashCode().Should().NotBe(second.GetHashCode());
    }
}
