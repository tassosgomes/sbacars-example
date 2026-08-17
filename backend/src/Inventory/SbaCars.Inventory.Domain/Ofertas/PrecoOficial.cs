using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Ofertas;

public sealed class PrecoOficial : ValueObject
{
    private PrecoOficial()
    {
        DefinidoPor = Autoria.System(DateTimeOffset.UnixEpoch);
    }

    public PrecoOficial(long valorCentavos, Autoria definidoPor, string moeda = "BRL")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valorCentavos);
        ArgumentNullException.ThrowIfNull(definidoPor);

        if (!string.Equals(moeda, "BRL", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("O preço oficial deve usar a moeda BRL.", nameof(moeda));
        }

        ValorCentavos = valorCentavos;
        Moeda = "BRL";
        DefinidoPor = definidoPor.Copiar();
    }

    public PrecoOficial(
        long valorCentavos,
        string usuarioId,
        string? nomeUsuario,
        DateTimeOffset definidoEm)
        : this(valorCentavos, new Autoria(usuarioId, nomeUsuario, definidoEm))
    {
    }

    public long ValorCentavos { get; private set; }

    public string Moeda { get; private set; } = "BRL";

    public Autoria DefinidoPor { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents() =>
        [ValorCentavos, Moeda, DefinidoPor];
}
