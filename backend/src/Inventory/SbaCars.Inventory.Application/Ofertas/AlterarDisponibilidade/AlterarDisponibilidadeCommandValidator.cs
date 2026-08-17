using FluentValidation;

namespace SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;

public sealed class AlterarDisponibilidadeCommandValidator
    : AbstractValidator<AlterarDisponibilidadeCommand>
{
    public AlterarDisponibilidadeCommandValidator()
    {
        RuleFor(command => command.OfertaId)
            .NotEmpty()
            .WithMessage("ofertaId é obrigatório.");

        RuleFor(command => command.NovoEstado)
            .IsInEnum()
            .WithMessage("novoEstado não é suportado.");

        RuleFor(command => command.Observacao)
            .MaximumLength(1000)
            .WithMessage("observacao deve ter no máximo 1000 caracteres.");
    }
}
