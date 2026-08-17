using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Domain.Ofertas;

public sealed class Disponibilidade : ValueObject
{
    private Disponibilidade()
    {
        AlteradaPor = Autoria.System(DateTimeOffset.UnixEpoch);
    }

    private Disponibilidade(
        EstadoDisponibilidade estado,
        DateTimeOffset desde,
        Autoria alteradaPor,
        bool estadoConhecido)
    {
        Estado = estado;
        Desde = desde.ToUniversalTime();
        AlteradaPor = alteradaPor;
        EstadoConhecido = estadoConhecido;
    }

    public EstadoDisponibilidade Estado { get; private set; }

    public DateTimeOffset Desde { get; private set; }

    public Autoria AlteradaPor { get; private set; }

    public bool EstadoConhecido { get; private set; }

    public static Disponibilidade Inicial(Autoria autoria) =>
        new(EstadoDisponibilidade.Disponivel, autoria.Em, autoria.Copiar(), false);

    public IReadOnlyCollection<EstadoDisponibilidade> TransicoesPermitidas => Estado switch
    {
        EstadoDisponibilidade.Disponivel =>
            [EstadoDisponibilidade.Reservado, EstadoDisponibilidade.Vendido],
        EstadoDisponibilidade.Reservado =>
            [EstadoDisponibilidade.Disponivel, EstadoDisponibilidade.Vendido],
        EstadoDisponibilidade.Vendido => [],
        _ => [],
    };

    /// <summary>
    /// Applies one explicit operational transition. There is intentionally no timer or expiry
    /// path here: a reservation remains reserved until an operator records a new state.
    /// </summary>
    public void Alterar(
        EstadoDisponibilidade novoEstado,
        Autoria autoria,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        if (!TransicoesPermitidas.Contains(novoEstado))
        {
            throw new TransicaoInvalidaException(Estado, novoEstado);
        }

        Estado = novoEstado;
        Desde = agora.ToUniversalTime();
        AlteradaPor = autoria.Copiar();
        EstadoConhecido = true;
    }

    /// <summary>
    /// Applies the exceptional sold-to-available transition. This operation is intentionally
    /// separate from <see cref="Alterar"/> and is called only after a reversal request is
    /// approved by the validation use case.
    /// </summary>
    public void ReverterVenda(Autoria autoria, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        if (Estado != EstadoDisponibilidade.Vendido)
        {
            throw new TransicaoInvalidaException(Estado, EstadoDisponibilidade.Disponivel);
        }

        Estado = EstadoDisponibilidade.Disponivel;
        Desde = agora.ToUniversalTime();
        AlteradaPor = autoria.Copiar();
        EstadoConhecido = true;
    }

    protected override IEnumerable<object?> GetEqualityComponents() =>
        [Estado, Desde, AlteradaPor, EstadoConhecido];
}
