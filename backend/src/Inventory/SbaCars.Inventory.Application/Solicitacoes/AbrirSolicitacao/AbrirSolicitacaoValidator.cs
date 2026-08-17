using FluentValidation;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Solicitacoes.AbrirSolicitacao;

public sealed class AbrirSolicitacaoValidator : AbstractValidator<AbrirSolicitacaoCommand>
{
    public AbrirSolicitacaoValidator()
    {
        RuleFor(command => command.OfertaId)
            .NotEmpty()
            .WithMessage("ofertaId é obrigatório.");

        RuleFor(command => command.Tipo)
            .IsInEnum()
            .WithMessage("tipo de solicitação não é suportado.");

        RuleFor(command => command.Justificativa)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage("justificativa é obrigatória e deve ter no máximo 1000 caracteres.");

        RuleFor(command => command.NovoPrecoCentavos)
            .GreaterThan(0)
            .When(command => command.Tipo == TipoSolicitacao.Preco)
            .WithMessage("novoPrecoCentavos deve ser maior que zero para uma solicitação de preço.");

        RuleFor(command => command.NovoPrecoCentavos)
            .Null()
            .When(command => command.Tipo != TipoSolicitacao.Preco)
            .WithMessage("novoPrecoCentavos só pode ser informado em uma solicitação de preço.");
    }
}
