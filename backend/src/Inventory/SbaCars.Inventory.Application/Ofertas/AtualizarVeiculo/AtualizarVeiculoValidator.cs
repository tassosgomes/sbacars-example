using FluentValidation;

namespace SbaCars.Inventory.Application.Ofertas.AtualizarVeiculo;

public sealed class AtualizarVeiculoValidator : AbstractValidator<AtualizarVeiculoCommand>
{
    private const string PlatePattern = "^[A-Za-z]{3}-?[0-9A-Za-z][A-Za-z0-9][0-9]{2}$";

    public AtualizarVeiculoValidator()
    {
        RuleFor(command => command)
            .Must(command => command.TemAlteracoes)
            .WithMessage("Informe ao menos um campo para atualizar.");

        RuleFor(command => command.TipoVeiculo)
            .NotEmpty()
            .When(command => command.TipoVeiculoInformado)
            .WithMessage("tipoVeiculo não pode ser nulo.");

        RuleFor(command => command.Placa)
            .Matches(PlatePattern)
            .When(command => command.PlacaInformada && !string.IsNullOrWhiteSpace(command.Placa))
            .WithMessage("placa deve seguir o padrão brasileiro.");

        RuleFor(command => command.AnoFabricacao)
            .InclusiveBetween(1950, 2100)
            .When(command => command.AnoFabricacaoInformado && command.AnoFabricacao.HasValue);

        RuleFor(command => command.AnoModelo)
            .InclusiveBetween(1950, 2100)
            .When(command => command.AnoModeloInformado && command.AnoModelo.HasValue);

        RuleFor(command => command.Quilometragem)
            .GreaterThanOrEqualTo(0)
            .When(command => command.QuilometragemInformada && command.Quilometragem.HasValue);

        RuleFor(command => command.Localizacao!.Cep)
            .Matches("^\\d{5}-?\\d{3}$")
            .When(command => command.LocalizacaoInformada &&
                             command.Localizacao is { CepInformado: true } location &&
                             !string.IsNullOrWhiteSpace(location.Cep))
            .WithMessage("cep deve conter oito dígitos.");

        RuleFor(command => command.Localizacao!.Uf)
            .Matches("^[A-Za-z]{2}$")
            .When(command => command.LocalizacaoInformada &&
                             command.Localizacao is { UfInformada: true } location &&
                             !string.IsNullOrWhiteSpace(location.Uf))
            .WithMessage("uf deve conter duas letras.");
    }
}
