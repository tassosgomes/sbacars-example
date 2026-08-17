using FluentValidation;

namespace SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;

public sealed class AprovarSolicitacaoValidator : AbstractValidator<AprovarSolicitacaoCommand>
{
    public AprovarSolicitacaoValidator()
    {
        RuleFor(command => command.SolicitacaoId)
            .NotEmpty()
            .WithMessage("solicitacaoId é obrigatório.");

        RuleFor(command => command.Observacao)
            .MaximumLength(1000)
            .WithMessage("observacao deve ter no máximo 1000 caracteres.");
    }
}
