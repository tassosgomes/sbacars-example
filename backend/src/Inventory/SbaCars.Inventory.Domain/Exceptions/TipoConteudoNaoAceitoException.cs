using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class TipoConteudoNaoAceitoException(string tipoConteudo)
    : DomainException($"O tipo de conteúdo '{tipoConteudo}' não é aceito para evidências.")
{
    public string TipoConteudo { get; } = tipoConteudo;
}
