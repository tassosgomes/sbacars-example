using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.CadastrarVeiculo;

public sealed class CadastrarVeiculoHandler(
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork,
    IEstoqueIntegrationEventPublisher integrationEventPublisher,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<CadastrarVeiculoCommand, OfertaDetalheResponse>
{
    public async Task<OfertaDetalheResponse> HandleAsync(
        CadastrarVeiculoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tipoVeiculo = TipoVeiculoExtensions.ParseTipoVeiculo(command.TipoVeiculo);
        var placa = NormalizePlate(command.Placa);

        if (placa is not null && await ofertaRepository
                .ExistePlacaAtivaAsync(placa, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            throw new PlacaDuplicadaException(placa);
        }

        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);
        var localizacao = command.Localizacao is null
            ? Localizacao.Vazia()
            : new Localizacao(
                command.Localizacao.Cep,
                command.Localizacao.Cidade,
                command.Localizacao.Uf);
        var veiculo = new Veiculo(
            tipoVeiculo,
            placa,
            command.Chassi,
            command.Marca,
            command.Modelo,
            command.Versao,
            command.AnoFabricacao,
            command.AnoModelo,
            command.Quilometragem,
            command.Cor,
            command.Combustivel,
            command.Cambio,
            localizacao);
        var oferta = Oferta.Criar(veiculo, autoria, agora);

        ofertaRepository.Adicionar(oferta);
        await integrationEventPublisher.PublishOfferIncludedAsync(
            oferta.Id,
            agora,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OfertaResponseMapper.ToDetalhe(oferta);
    }

    private static string? NormalizePlate(string? placa) =>
        string.IsNullOrWhiteSpace(placa)
            ? null
            : placa.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
