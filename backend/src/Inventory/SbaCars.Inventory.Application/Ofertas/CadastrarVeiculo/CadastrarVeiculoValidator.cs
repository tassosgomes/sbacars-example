using FluentValidation;

namespace SbaCars.Inventory.Application.Ofertas.CadastrarVeiculo;

public sealed class CadastrarVeiculoValidator : AbstractValidator<CadastrarVeiculoCommand>
{
    private const string PlatePattern = "^[A-Za-z]{3}-?[0-9A-Za-z][A-Za-z0-9][0-9]{2}$";

    public CadastrarVeiculoValidator()
    {
        RuleFor(command => command.TipoVeiculo)
            .NotEmpty()
            .WithMessage("tipoVeiculo é obrigatório.");

        RuleFor(command => command.Placa)
            .Matches(PlatePattern)
            .When(command => !string.IsNullOrWhiteSpace(command.Placa))
            .WithMessage("placa deve seguir o padrão brasileiro.");

        RuleFor(command => command.AnoFabricacao)
            .InclusiveBetween(1950, 2100)
            .When(command => command.AnoFabricacao.HasValue);

        RuleFor(command => command.AnoModelo)
            .InclusiveBetween(1950, 2100)
            .When(command => command.AnoModelo.HasValue);

        RuleFor(command => command.Quilometragem)
            .GreaterThanOrEqualTo(0)
            .When(command => command.Quilometragem.HasValue);

        RuleFor(command => command.Localizacao!.Uf)
            .Matches("^[A-Za-z]{2}$")
            .When(command => !string.IsNullOrWhiteSpace(command.Localizacao?.Uf))
            .WithMessage("uf deve conter duas letras.");
    }
}
