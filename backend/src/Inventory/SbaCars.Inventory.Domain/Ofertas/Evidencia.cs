using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>Metadata for an evidence file stored in object storage; bytes never transit the API.</summary>
public sealed class Evidencia : Entity
{
    public const long MaxTamanhoBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> TiposConteudoAceitos = new(StringComparer.Ordinal)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
    };

    private Evidencia()
    {
        ObjectKey = string.Empty;
        NomeArquivo = string.Empty;
        TipoConteudo = string.Empty;
        EnviadaPor = Autoria.System(DateTimeOffset.UnixEpoch);
    }

    public Guid OfertaId { get; private set; }

    public string ObjectKey { get; private set; }

    public string NomeArquivo { get; private set; }

    public string TipoConteudo { get; private set; }

    public long TamanhoBytes { get; private set; }

    public string? Checksum { get; private set; }

    public Autoria EnviadaPor { get; private set; }

    public DateTimeOffset EnviadaEm { get; private set; }

    public static Evidencia Criar(
        Guid ofertaId,
        string nomeArquivo,
        string tipoConteudo,
        long tamanhoBytes,
        Autoria enviadaPor,
        DateTimeOffset enviadaEm)
    {
        if (ofertaId == Guid.Empty)
        {
            throw new ArgumentException("O id da oferta é obrigatório.", nameof(ofertaId));
        }

        ArgumentNullException.ThrowIfNull(enviadaPor);
        ValidarTipoConteudo(tipoConteudo);
        ValidarTamanho(tamanhoBytes);

        var nomeSeguro = SanitizarNomeArquivo(nomeArquivo);
        var evidencia = new Evidencia
        {
            OfertaId = ofertaId,
            NomeArquivo = nomeSeguro,
            TipoConteudo = tipoConteudo.Trim(),
            TamanhoBytes = tamanhoBytes,
            Checksum = null,
            EnviadaPor = enviadaPor.Copiar(),
            EnviadaEm = enviadaEm.ToUniversalTime(),
        };
        evidencia.ObjectKey = ConstruirObjectKey(ofertaId, evidencia.Id, nomeSeguro);
        return evidencia;
    }

    internal static void ValidarTipoConteudo(string tipoConteudo)
    {
        if (string.IsNullOrWhiteSpace(tipoConteudo) ||
            !TiposConteudoAceitos.Contains(tipoConteudo.Trim()))
        {
            throw new TipoConteudoNaoAceitoException(tipoConteudo ?? string.Empty);
        }
    }

    internal static void ValidarTamanho(long tamanhoBytes)
    {
        if (tamanhoBytes < 1 || tamanhoBytes > MaxTamanhoBytes)
        {
            throw new ArquivoExcedeTamanhoException(tamanhoBytes, MaxTamanhoBytes);
        }
    }

    internal static string SanitizarNomeArquivo(string nomeArquivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeArquivo);

        var trimmed = nomeArquivo.Trim();
        var normalized = trimmed.Replace('\\', '/');
        var lastSegment = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastSegment) || lastSegment.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("O nome do arquivo é inválido.", nameof(nomeArquivo));
        }

        return lastSegment;
    }

    internal static string ConstruirObjectKey(Guid ofertaId, Guid evidenciaId, string nomeArquivo) =>
        $"ofertas/{ofertaId:N}/evidencias/{evidenciaId:N}/{nomeArquivo}";
}
