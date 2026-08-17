using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class ArquivoExcedeTamanhoException(long tamanhoBytes, long maximoBytes)
    : DomainException($"O arquivo de {tamanhoBytes} bytes excede o limite de {maximoBytes} bytes.")
{
    public long TamanhoBytes { get; } = tamanhoBytes;

    public long MaximoBytes { get; } = maximoBytes;
}
