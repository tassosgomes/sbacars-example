using FluentValidation;

namespace SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;

public sealed class ListarFilaValidacaoValidator : AbstractValidator<ListarFilaValidacaoQuery>
{
    private static readonly string[] SortOptions =
    [
        "abertaEm:asc",
        "abertaEm:desc",
        "decididaEm:desc",
    ];

    public ListarFilaValidacaoValidator()
    {
        RuleFor(query => query.Status)
            .Must(value => value is null or "pendente" or "aprovada" or "rejeitada")
            .WithMessage("status não é suportado.");

        RuleForEach(query => query.Tipo)
            .Must(value => value is "elegibilidade" or "preco" or "retirada" or "reversao-venda")
            .WithMessage("tipo não é suportado.");

        RuleFor(query => query.OrdenarPor)
            .Must(value => SortOptions.Contains(value, StringComparer.Ordinal))
            .WithMessage("ordenarPor não é suportado.");
    }
}
