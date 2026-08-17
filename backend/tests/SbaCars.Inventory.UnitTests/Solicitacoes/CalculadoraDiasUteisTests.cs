using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.UnitTests.Solicitacoes;

public sealed class CalculadoraDiasUteisTests
{
    private readonly CalculadoraDiasUteis _calculadora = new();

    [Fact]
    public void ForaDoSla_ExactlyOneBusinessDay_ReturnsFalse()
    {
        var abertura = new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.Zero); // Friday
        var agora = new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero); // Monday

        _calculadora.ForaDoSla(abertura, agora).Should().BeFalse();
        _calculadora.CalcularTempoUtil(abertura, agora).Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void ForaDoSla_TwentyFiveBusinessHoursAcrossWeekend_ReturnsTrue()
    {
        var abertura = new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.Zero); // Friday
        var agora = new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero); // Monday

        _calculadora.ForaDoSla(abertura, agora).Should().BeTrue();
        _calculadora.CalcularTempoUtil(abertura, agora).Should().Be(TimeSpan.FromHours(25));
    }

    [Fact]
    public void CalcularTempoUtil_WeekendOnly_ReturnsZero()
    {
        var abertura = new DateTimeOffset(2026, 8, 15, 9, 0, 0, TimeSpan.Zero); // Saturday
        var agora = new DateTimeOffset(2026, 8, 16, 18, 0, 0, TimeSpan.Zero); // Sunday

        _calculadora.CalcularTempoUtil(abertura, agora).Should().Be(TimeSpan.Zero);
        _calculadora.ForaDoSla(abertura, agora).Should().BeFalse();
    }
}
