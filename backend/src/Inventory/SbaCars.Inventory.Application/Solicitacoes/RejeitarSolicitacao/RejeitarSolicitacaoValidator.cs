using FluentValidation;

namespace SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;

public sealed class RejeitarSolicitacaoValidator : AbstractValidator<RejeitarSolicitacaoCommand>
{
    public RejeitarSolicitacaoValidator()
    {
        RuleFor(command => command.SolicitacaoId)
            .NotEmpty()
            .WithMessage("solicitacaoId é obrigatório.");

        RuleFor(command => command.Justificativa)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage("justificativa é obrigatória e deve ter no máximo 1000 caracteres.");
    }
}
