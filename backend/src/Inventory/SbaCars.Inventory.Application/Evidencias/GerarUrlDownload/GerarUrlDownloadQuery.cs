using SbaCars.BuildingBlocks.Application.Cqrs;

namespace SbaCars.Inventory.Application.Evidencias.GerarUrlDownload;

public sealed record GerarUrlDownloadQuery(Guid EvidenciaId) : IQuery<DownloadEvidenciaResponse>;
