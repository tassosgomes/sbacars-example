using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.UnitTests;

public class DomainExceptionTests
{
    private sealed class SampleDomainException : DomainException
    {
        public SampleDomainException(string message)
            : base(message)
        {
        }

        public SampleDomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new SampleDomainException("regra de negócio violada");

        // Assert
        exception.Message.Should().Be("regra de negócio violada");
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("causa raiz");

        // Act
        var exception = new SampleDomainException("regra de negócio violada", inner);

        // Assert
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void DomainException_IsAnException()
    {
        // Arrange & Act
        var exception = new SampleDomainException("falha esperada");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}
