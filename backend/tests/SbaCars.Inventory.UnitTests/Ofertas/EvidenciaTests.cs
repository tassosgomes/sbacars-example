using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class EvidenciaTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

  private static readonly Autoria Autoria =
        new("operator-42", "Ana Souza", Now);

    [Fact]
    public void Criar_TipoConteudoInvalido_LancaTipoConteudoNaoAceitoException()
    {
        var act = () => Evidencia.Criar(
            Guid.CreateVersion7(),
            "malware.exe",
            "application/x-msdownload",
            1024,
            Autoria,
            Now);

        act.Should().Throw<TipoConteudoNaoAceitoException>();
    }

    [Fact]
    public void Criar_TamanhoOnzeMiB_LancaArquivoExcedeTamanhoException()
    {
        const long onzeMiB = 11 * 1024 * 1024;

        var act = () => Evidencia.Criar(
            Guid.CreateVersion7(),
            "laudo.pdf",
            "application/pdf",
            onzeMiB,
            Autoria,
            Now);

        act.Should().Throw<ArquivoExcedeTamanhoException>();
    }

    [Fact]
    public void Criar_PdfQuatroMiB_CriaEntidadeComMetadados()
    {
        var ofertaId = Guid.CreateVersion7();
        const long quatroMiB = 4 * 1024 * 1024;

        var evidencia = Evidencia.Criar(
            ofertaId,
            "laudo-cautelar.pdf",
            "application/pdf",
            quatroMiB,
            Autoria,
            Now);

        evidencia.OfertaId.Should().Be(ofertaId);
        evidencia.NomeArquivo.Should().Be("laudo-cautelar.pdf");
        evidencia.TipoConteudo.Should().Be("application/pdf");
        evidencia.TamanhoBytes.Should().Be(quatroMiB);
        evidencia.Checksum.Should().BeNull();
        evidencia.EnviadaPor.Should().Be(Autoria);
        evidencia.EnviadaEm.Should().Be(Now);
        evidencia.ObjectKey.Should().Be(
            $"ofertas/{ofertaId:N}/evidencias/{evidencia.Id:N}/laudo-cautelar.pdf");
    }

    [Fact]
    public void BlocoFatoIndisponivel_DescartaEvidenciaId()
    {
        var evidenciaId = Guid.CreateVersion7();
        var fato = new BlocoFato(
            BlocoFatoTipo.Origem,
            indisponivel: true,
            limitacaoDeclarada: "Sem laudo disponível.",
            evidenciaId: evidenciaId);

        fato.EvidenciaId.Should().BeNull();
    }

    [Fact]
    public void BlocoFato_ComEvidenciaIdDiferente_NaoSaoIguais()
    {
        var autoria = Autoria;
        var evidenciaId = Guid.CreateVersion7();
        var semEvidencia = new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem", atualizadoPor: autoria);
        var comEvidencia = new BlocoFato(
            BlocoFatoTipo.Origem,
            descricao: "Origem",
            evidenciaId: evidenciaId,
            atualizadoPor: autoria);

        semEvidencia.Equals(comEvidencia).Should().BeFalse();
    }
}
