using FluentValidation;

namespace SbaCars.Inventory.Application.Ofertas.ListarOfertas;

public sealed class ListarOfertasValidator : AbstractValidator<ListarOfertasQuery>
{
    private static readonly string[] SortOptions =
    [
        "atualizadoEm:desc",
        "atualizadoEm:asc",
        "precoOficialCentavos:desc",
        "precoOficialCentavos:asc",
        "veiculo:asc",
    ];

    public ListarOfertasValidator()
    {
        RuleFor(query => query.Busca)
            .MinimumLength(2)
            .When(query => !string.IsNullOrWhiteSpace(query.Busca));

        RuleFor(query => query.Uf)
            .Matches("^[A-Za-z]{2}$")
            .When(query => !string.IsNullOrWhiteSpace(query.Uf));

        RuleFor(query => query.OrdenarPor)
            .Must(value => SortOptions.Contains(value, StringComparer.Ordinal))
            .WithMessage("ordenarPor não é suportado.");

        RuleForEach(query => query.Situacao)
            .Must(IsSituacao)
            .WithMessage("situacao não é suportada.");

        RuleForEach(query => query.Disponibilidade)
            .Must(IsDisponibilidade)
            .WithMessage("disponibilidade não é suportada.");
    }

    private static bool IsSituacao(string value) =>
        value is "em-preparacao" or "elegivel" or "suspensa" or "retirada";

    private static bool IsDisponibilidade(string value) =>
        value is "disponivel" or "reservado" or "vendido";
}
