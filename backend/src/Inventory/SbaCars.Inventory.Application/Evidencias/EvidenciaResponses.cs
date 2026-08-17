namespace SbaCars.Inventory.Application.Evidencias;

public sealed record UploadEvidenciaResponse(
    Guid EvidenciaId,
    string UploadUrl,
    DateTimeOffset ExpiraEm,
    IReadOnlyDictionary<string, string> HeadersObrigatorios);

public sealed record DownloadEvidenciaResponse(
    string DownloadUrl,
    DateTimeOffset ExpiraEm,
    string NomeArquivo);
