using FluentValidation;

namespace SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;

public sealed class DefinirPrecoInicialCommandValidator
    : AbstractValidator<DefinirPrecoInicialCommand>
{
    public DefinirPrecoInicialCommandValidator()
    {
        RuleFor(command => command.OfertaId)
            .NotEmpty()
            .WithMessage("ofertaId é obrigatório.");

        RuleFor(command => command.ValorCentavos)
            .GreaterThan(0)
            .WithMessage("valorCentavos deve ser um inteiro maior que zero.");
    }
}
