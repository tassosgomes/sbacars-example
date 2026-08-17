using FluentValidation;

namespace SbaCars.Inventory.Application.Ofertas.SubstituirFatos;

public sealed class SubstituirFatosValidator : AbstractValidator<SubstituirFatosCommand>
{
    public SubstituirFatosValidator()
    {
        RuleFor(command => command.OfertaId)
            .NotEmpty()
            .WithMessage("ofertaId é obrigatório.");

        RuleFor(command => command.Origem)
            .NotNull()
            .WithMessage("origem é obrigatório.");

        RuleFor(command => command.Condicao)
            .NotNull()
            .WithMessage("condicao é obrigatório.");

        RuleFor(command => command.Historico)
            .NotNull()
            .WithMessage("historico é obrigatório.");

        RuleFor(command => command.Origem)
            .SetValidator(new BlocoFatoInputValidator()!);
        RuleFor(command => command.Condicao)
            .SetValidator(new BlocoFatoInputValidator()!);
        RuleFor(command => command.Historico)
            .SetValidator(new BlocoFatoInputValidator()!);
    }

    private sealed class BlocoFatoInputValidator : AbstractValidator<BlocoFatoInput>
    {
        public BlocoFatoInputValidator()
        {
            RuleFor(input => input.Descricao)
                .MaximumLength(2000)
                .WithMessage("descricao não pode exceder 2000 caracteres.");

            RuleFor(input => input.Fonte)
                .MaximumLength(300)
                .WithMessage("fonte não pode exceder 300 caracteres.");

            RuleFor(input => input.LimitacaoDeclarada)
                .MaximumLength(1000)
                .WithMessage("limitacaoDeclarada não pode exceder 1000 caracteres.");
        }
    }
}
